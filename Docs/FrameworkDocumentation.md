# Unity Gameplay Framework Documentation

## Overview

This project contains a small Unreal-inspired gameplay framework built on Unity and Netcode for GameObjects.

The framework separates these responsibilities:

| System | Responsibility |
| --- | --- |
| `UGameInstance` | Persistent application state, scene lifecycle, and high-level network lifecycle events. |
| `UGameMode` | Scene-level match rules, player registration, pawn possession, and player start selection. |
| `UObject` | Base networked gameplay object with identity, lifecycle, ticking, and authority helpers. |
| `UController` | Non-physical decision maker that possesses a pawn and drives control rotation/input. |
| `UPawn` | Controllable gameplay body that receives movement input and possession state. |
| `UComponent` | Actor-owned reusable behavior with activation, registration, ticking, and network lifecycle. |
| `UCharacterMovementComponent` | CharacterController-based movement simulation with networked velocity/movement mode replication. |

The current networking model expects `NetworkManager` to own player object creation. `UGameMode` does not spawn controllers or pawns directly. Instead, it listens to network events through a concrete subclass, receives already-created player objects, registers their controllers, and resolves existing pawns for possession.

---

## Architecture

```text
UGameInstance
  Persistent singleton
  Scene events
  NetworkManager lifecycle events

UGameMode
  Scene singleton
  Match start/end
  Player registration
  Pawn resolution and possession

UObject : NetworkBehaviour
  UController
  UPawn

UComponent : NetworkBehaviour
  UCharacterMovementComponent
```

### Networking Ownership

`NetworkManager` is the source of truth for network sessions and player object spawning.

Expected flow:

1. `FrameworkTestGameMode` ensures there is a `NetworkManager`.
2. It configures `NetworkManager.NetworkConfig.PlayerPrefab` from `DefaultControllerPrefab`.
3. Host/server startup is handled by `NetworkManager`.
4. `FrameworkTestGameMode` listens to `OnServerStarted`.
5. It subscribes to `OnClientConnectedCallback`.
6. On connection, it receives `NetworkClient.PlayerObject`.
7. It extracts a `UController` from the player object.
8. It registers the controller and resolves an existing pawn.
9. The controller possesses that pawn.

`UGameMode` should not be placed on the same GameObject as a `NetworkObject`. It is a scene authority `MonoBehaviour`, not a networked object.

---

## Core Framework Classes

## `UGameInstance`

File: `Assets/Scripts/Framework/UGameInstance.cs`

`UGameInstance` is a persistent application-level singleton. It survives scene changes with `DontDestroyOnLoad`.

### Responsibilities

- Own global game/application state.
- Track initialization and shutdown.
- Provide scene loading helpers.
- Forward active scene, scene loaded, and scene unloaded events.
- Register high-level `NetworkManager` callbacks when a singleton exists.

### Important Properties

| Property | Purpose |
| --- | --- |
| `Instance` | Current global game instance. |
| `HasInstance` | Whether an instance exists. |
| `IsInitialized` | Whether `Init()` has run. |
| `CurrentScene` | Current active Unity scene. |
| `CurrentLevelName` | Name of the current active scene. |

### Important Events

| Event | Fired When |
| --- | --- |
| `Initialized` | Initialization completes. |
| `ShuttingDown` | Shutdown begins. |
| `ActiveSceneChanged` | Unity active scene changes. |
| `LevelLoaded` | A scene is loaded. |
| `LevelUnloaded` | A scene is unloaded. |
| `NetworkStarted` | Server or client starts. |
| `NetworkStopped` | Server or client stops. |
| `ClientConnected` | Netcode reports a client connection. |
| `ClientDisconnected` | Netcode reports a client disconnection. |

### Override Points

| Method | Use For |
| --- | --- |
| `Init()` | One-time initialization. |
| `Shutdown()` | Cleanup before the instance is destroyed. |
| `OnStartGameInstance()` | Startup logic after network callbacks are registered. |
| `OnActiveSceneChanged()` | Responding to active scene changes. |
| `OnLevelLoaded()` | Scene load handling. |
| `OnLevelUnloaded()` | Scene unload handling. |
| `OnNetworkStarted()` | Reacting to host/server/client start. |
| `OnNetworkStopped()` | Reacting to network shutdown. |
| `OnClientConnected()` | Global client connection handling. |
| `OnClientDisconnected()` | Global client disconnection handling. |

---

## `UGameMode`

File: `Assets/Scripts/Framework/UGameMode.cs`

`UGameMode` is a scene-level rules authority. It owns match state, player registration, pawn lookup, and possession flow.

It is intentionally a `MonoBehaviour`, not a `NetworkBehaviour`. This avoids the invalid Netcode setup where a `NetworkManager` and `NetworkObject` live on the same GameObject.

### Responsibilities

- Maintain the active scene's game mode singleton.
- Start and end matches.
- Register and unregister controllers and pawns.
- Select player start transforms.
- Handle already-created network player objects.
- Resolve an existing pawn for a controller.
- Possess pawns through `UController`.

### Important Properties

| Property | Purpose |
| --- | --- |
| `Instance` | Current scene game mode. |
| `Controllers` | Registered controllers. |
| `Pawns` | Registered pawns. |
| `DefaultControllerPrefab` | Prefab reference used by concrete modes to configure `NetworkManager.PlayerPrefab`. |
| `DefaultPawnPrefab` | Prefab reference used by concrete modes for prefab registration or fallback rules. |
| `HasMatchStarted` | Whether the match has started. |
| `HasMatchEnded` | Whether the match has ended. |
| `HasNetworkAuthority` | True when there is no network session, no listening manager, or the local instance is server. |

### Match Flow

```text
Start()
  DispatchBeginPlay()
    BeginPlay()
      if authority and ReadyToStartMatch()
        StartMatch()
          OnMatchStarted()
          MatchStarted event
```

### Player Flow

```text
NetworkManager creates player object
  FrameworkTestGameMode receives NetworkClient.PlayerObject
    HandleNetworkPlayerObject(NetworkObject)
      ResolveControllerFromNetworkObject()
      HandleStartingNewPlayer(controller)
        RegisterController(controller)
        OnStartingNewPlayer(controller)
        RestartPlayer(controller)
          ChoosePlayerStart(controller)
          ResolvePawnForController(controller, startSpot)
          controller.Possess(pawn)
          controller.Restart()
```

### Override Points

| Method | Use For |
| --- | --- |
| `ReadyToStartMatch()` | Gate match startup. |
| `ReadyToEndMatch()` | Gate match shutdown. |
| `ChoosePlayerStart()` | Pick a spawn/start transform for a controller. |
| `OnStartingNewPlayer()` | React after a controller is registered but before restart. |
| `ResolvePawnForController()` | Return an existing pawn for possession. Do not spawn here unless changing the framework contract. |
| `OnRestartPlayer()` | React after restart attempt. |
| `ResolveControllerFromNetworkObject()` | Map a Netcode player object to a controller. |
| `HandleNetworkPlayerObject()` | Entry point for Netcode-created player objects. |
| `OnMatchStarted()` | Match start rules. |
| `OnMatchEnded()` | Match end rules. |

### Important Rule

`UGameMode` should coordinate. It should not own network object spawning.

Use `NetworkManager` for:

- Starting host/server/client.
- Creating player objects.
- Assigning the player prefab.
- Reporting connected clients.

Use `UGameMode` for:

- Interpreting the connected player object.
- Registering the controller.
- Selecting a player start.
- Assigning an existing pawn.
- Calling possession/restart hooks.

---

## `UObject`

File: `Assets/Scripts/Framework/UObject.cs`

`UObject` is the base class for networked gameplay objects. It extends `NetworkBehaviour`.

### Responsibilities

- Provide object identity.
- Provide BeginPlay/EndPlay-style lifecycle.
- Provide optional ticking.
- Provide network spawn/despawn lifecycle hooks.
- Provide authority and local-control helpers.
- Provide a safe destruction path for networked and non-networked objects.

### Important Properties

| Property | Purpose |
| --- | --- |
| `ObjectName` | Serialized/display object name. Defaults to GameObject name. |
| `ObjectGuid` | Runtime GUID generated in `Awake()`. |
| `Outer` | Optional owning `UObject`. |
| `HasBegunPlay` | Whether BeginPlay has dispatched. |
| `CanEverTick` | Enables `Tick(deltaTime)`. |
| `IsPendingKill` | Set when destruction has started. |
| `IsNetworked` | Whether a `NetworkObject` exists. |
| `HasNetworkAuthority` | True for non-networked, unspawned, or server-owned logic. |
| `HasLocalControl` | True for non-networked, unspawned, or owner-controlled logic. |

### Lifecycle

```text
Awake()
  ObjectGuid assigned
  ObjectName defaulted

Start()
  If not networked/spawned:
    DispatchBeginPlay()

OnNetworkSpawn()
  DispatchBeginPlay()
  NetworkSpawned()

OnNetworkDespawn()
  NetworkDespawned()
  DispatchEndPlay()

Update()
  If CanEverTick and begun play:
    Tick(deltaTime)

OnDestroy()
  DispatchEndPlay()
  Destroyed event
```

### Override Points

| Method | Use For |
| --- | --- |
| `BeginPlay()` | First active gameplay initialization. |
| `EndPlay()` | Gameplay cleanup. |
| `Tick(float deltaTime)` | Per-frame behavior when `CanEverTick` is true. |
| `NetworkSpawned()` | Network-specific spawn behavior. |
| `NetworkDespawned()` | Network-specific despawn behavior. |

---

## `UController`

File: `Assets/Scripts/Framework/UController.cs`

`UController` is a non-physical decision maker. It possesses a `UPawn`, drives control rotation, and replicates possession/control state.

### Responsibilities

- Track the possessed pawn.
- Possess and unpossess pawns.
- Replicate pawn reference from server to clients.
- Replicate control rotation from owner to everyone.
- Provide input helper methods for yaw, pitch, and roll.
- Expose restart hooks.

### Important Properties

| Property | Purpose |
| --- | --- |
| `Pawn` | Currently possessed pawn. |
| `ControlRotation` | Controller's current rotation intent. |
| `HasPawn` | Whether a pawn is possessed. |
| `IsPlayerController` | Override to identify player controllers. |

### Networking

| Data | Replication |
| --- | --- |
| `pawnReference` | Written by server, read by everyone. |
| `replicatedControlRotation` | Written by owner, read by everyone. |

Possession should be authoritative. If a spawned non-server instance calls `Possess`, it requests possession through a server RPC.

### Override Points

| Method | Use For |
| --- | --- |
| `OnPossess(UPawn)` | React after pawn possession. |
| `OnUnPossess(UPawn)` | React after pawn release. |
| `OnRestart()` | Reset or refresh controller state after restart. |

---

## `UPawn`

File: `Assets/Scripts/Framework/UPawn.cs`

`UPawn` is the controllable gameplay body. It stores movement input, tracks its controller, and applies controller rotation rules.

### Responsibilities

- Track current controller.
- Replicate controller reference from server to clients.
- Accumulate movement input.
- Expose pending and last movement input vectors.
- Apply control rotation to pawn rotation.

### Important Properties

| Property | Purpose |
| --- | --- |
| `Controller` | Current controller. |
| `IsPossessed` | Whether a controller is assigned. |
| `UseControllerRotationYaw` | Whether yaw follows controller rotation. |
| `UseControllerRotationPitch` | Whether pitch follows controller rotation. |
| `UseControllerRotationRoll` | Whether roll follows controller rotation. |

### Movement Input

Use `AddMovementInput(worldDirection, scaleValue)` to accumulate input. Movement components consume this with `ConsumeMovementInputVector()`.

### Override Points

| Method | Use For |
| --- | --- |
| `OnPossessed(UController)` | React when a controller takes ownership. |
| `OnUnPossessed(UController)` | React when a controller releases the pawn. |
| `FaceRotation(Quaternion)` | Customize how control rotation affects the pawn. |

---

## `UComponent`

File: `Assets/Scripts/Framework/UComponent.cs`

`UComponent` is an actor-owned reusable behavior. It extends `NetworkBehaviour`.

### Responsibilities

- Resolve an owning `UObject`.
- Register/unregister itself.
- Activate/deactivate itself.
- Provide BeginPlay/EndPlay-style lifecycle.
- Provide optional ticking.
- Provide network lifecycle hooks.

### Important Properties

| Property | Purpose |
| --- | --- |
| `Owner` | Nearest `UObject` on the same GameObject or a parent. |
| `IsActive` | Whether the component is active. |
| `IsRegistered` | Whether registration has completed. |
| `HasBegunPlay` | Whether BeginPlay has dispatched. |
| `CanEverTick` | Enables `TickComponent(deltaTime)`. |
| `HasNetworkAuthority` | True for non-networked, unspawned, or server-owned logic. |
| `HasLocalControl` | True for non-networked, unspawned, or owner-controlled logic. |

### Lifecycle

```text
Awake()
  RegisterComponent()

Start()
  Activate() if autoActivate
  DispatchBeginPlay() if not network-spawned

OnNetworkSpawn()
  RegisterComponent()
  DispatchBeginPlay()
  NetworkSpawned()

Update()
  If active, ticking, and begun play:
    TickComponent(deltaTime)

OnDestroy()
  DispatchEndPlay()
  UnregisterComponent()
```

### Override Points

| Method | Use For |
| --- | --- |
| `OnRegister()` | Cache owner dependencies. |
| `OnUnregister()` | Cleanup registration state. |
| `BeginPlay()` | Runtime initialization. |
| `EndPlay()` | Runtime cleanup. |
| `TickComponent(float)` | Per-frame behavior. |
| `OnActivated()` | Activation behavior. |
| `OnDeactivated()` | Deactivation behavior. |
| `NetworkSpawned()` | Network-specific spawn behavior. |
| `NetworkDespawned()` | Network-specific despawn behavior. |

---

## `UCharacterMovementComponent`

File: `Assets/Scripts/Components/UCharacterMovementComponent.cs`

`UCharacterMovementComponent` moves a pawn using Unity's `CharacterController`.

### Requirements

- `CharacterController`
- A `UPawn` owner on the same GameObject or a parent

### Movement Modes

| Mode | Behavior |
| --- | --- |
| `None` | No movement simulation. |
| `Walking` | Grounded movement with gravity and jump support. |
| `Falling` | Gravity-driven movement. |
| `Flying` | Direct velocity movement without gravity. |

### Responsibilities

- Consume pawn movement input.
- Simulate velocity, acceleration, braking, gravity, and jumping.
- Move the `CharacterController`.
- Rotate toward movement when enabled.
- Replicate velocity and movement mode from server to clients.
- Accept owner input from clients through server RPC.

### Networking

| Data | Replication |
| --- | --- |
| `replicatedVelocity` | Written by server, read by everyone. |
| `replicatedMovementMode` | Written by server, read by everyone. |

Client movement flow:

```text
Local owner gathers input
  TickComponent()
    RequestMovementInputRpc(input, wantsJump)
      Server simulates movement
      Server writes replicated velocity/mode
      Clients receive replicated state
```

### Main Tuning Fields

| Field | Purpose |
| --- | --- |
| `defaultMovementMode` | Initial movement mode on BeginPlay. |
| `maxWalkSpeed` | Walking speed cap. |
| `maxFlySpeed` | Flying speed cap. |
| `acceleration` | Rate of speeding up. |
| `brakingDecelerationWalking` | Walking slowdown rate. |
| `brakingDecelerationFlying` | Flying slowdown rate. |
| `orientRotationToMovement` | Rotate toward movement direction. |
| `rotationRate` | Degrees per second for rotation. |
| `gravityScale` | Gravity multiplier. |
| `jumpZVelocity` | Initial vertical jump velocity. |
| `maxFallSpeed` | Downward velocity cap. |
| `groundedStickForce` | Small downward force while grounded. |

---

## Framework Test Implementation

## `FrameworkTestGameInstance`

File: `Assets/Scripts/FrameworkTest/FrameworkTestGameInstance.cs`

Concrete `UGameInstance` used by the framework test scene.

### Behavior

- Sets `Application.targetFrameRate` to `120`.
- Optionally starts host mode when `StartGameInstance()` runs.
- Shuts down the network session when returning to main menu.

---

## `FrameworkTestGameMode`

File: `Assets/Scripts/FrameworkTest/FrameworkTestGameMode.cs`

Concrete `UGameMode` for the test framework.

### Responsibilities

- Ensure a `NetworkManager` exists.
- Ensure a `UnityTransport` exists.
- Assign `DefaultControllerPrefab` as `NetworkManager.NetworkConfig.PlayerPrefab`.
- Register `DefaultPawnPrefab` as a network prefab if one is configured.
- Start the game instance.
- Optionally start host mode on BeginPlay.
- Listen to server/client connection callbacks.
- Register connected player controllers.
- Resolve an existing pawn for a controller.

### Network Callback Flow

```text
Awake()
  EnsureNetworkManager()
  Subscribe to OnServerStarted
  EnsureGameInstance()
  ConfigureNetworkManager()

BeginPlay()
  gameInstance.StartGameInstance()
  optionally StartHost()
  RegisterServerCallbacks()
  base.BeginPlay()

OnServerStarted()
  RegisterServerCallbacks()
  StartMatch() if ready

OnClientConnectedCallback(clientId)
  ConnectedClients[clientId].PlayerObject
  HandleNetworkPlayerObject(playerObject)
```

### Pawn Resolution

`FrameworkTestGameMode.ResolvePawnForController()` uses this order:

1. Use a `UPawn` found under the controller object.
2. Otherwise find the closest unpossessed `UPawn` in the scene.
3. If networking is active, ignore pawns with an unspawned `NetworkObject`.

The method does not instantiate or spawn pawns.

### Important Setup Notes

- `DefaultControllerPrefab` should be assigned to a prefab with:
  - `NetworkObject`
  - `UController` subclass
- `DefaultPawnPrefab` may be registered as a Netcode prefab, but this game mode does not spawn it directly.
- At runtime, pawns must already exist or be created by another network-aware system.
- `GameMode` should not have a `NetworkObject`.
- `NetworkManager` can be placed separately in the scene or created by the test game mode.

---

## `FrameworkTestPlayerController`

File: `Assets/Scripts/FrameworkTest/FrameworkTestPlayerController.cs`

Concrete first-person player controller.

### Responsibilities

- Identifies itself as a player controller.
- Reads keyboard movement input.
- Reads mouse look input.
- Sends input into the possessed `FrameworkTestPlayerPawn`.
- Handles jump input.
- Locks/hides cursor for local control.

### Input Mapping

| Input | Action |
| --- | --- |
| `W` | Move forward. |
| `S` | Move backward. |
| `A` | Move left. |
| `D` | Move right. |
| `Mouse delta` | Look around. |
| `Space` | Jump. |

Only the local owner processes input when network-spawned.

---

## `FrameworkTestPlayerPawn`

File: `Assets/Scripts/FrameworkTest/FrameworkTestPlayerPawn.cs`

Concrete first-person pawn.

### Requirements

- `NetworkObject`
- `CharacterController`
- `UCharacterMovementComponent`

### Responsibilities

- Own first-person movement behavior.
- Ensure a camera root exists.
- Ensure a player camera exists.
- Ensure an audio listener exists.
- Enable camera/audio only for the local view.
- Convert 2D movement input into world movement input.
- Apply mouse look pitch/yaw.
- Forward jump requests to `UCharacterMovementComponent`.

### Local View Rules

The pawn enables camera and audio when:

- It is not network-spawned, or
- It is the owner, or
- It has local control.

Remote players should not render active cameras or audio listeners.

---

## Creating a New Gameplay Object

Use `UObject` when the thing is a networked gameplay entity.

```csharp
using Framework;
using UnityEngine;

public class MyActor : UObject
{
    protected override void Awake()
    {
        base.Awake();
        CanEverTick = true;
    }

    protected override void BeginPlay()
    {
        base.BeginPlay();
    }

    protected override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);
    }
}
```

Checklist:

- Add `NetworkObject` if the object should be network-spawned.
- Put server-only logic behind `HasNetworkAuthority` or `IsServer`.
- Put local input/view logic behind `HasLocalControl` or `IsOwner`.
- Use `DestroyObject()` instead of directly destroying networked objects.

---

## Creating a New Component

Use `UComponent` for reusable behavior attached to a `UObject`.

```csharp
using Framework;

public class MyComponent : UComponent
{
    private UPawn pawn;

    protected override void OnRegister()
    {
        base.OnRegister();
        pawn = GetOwner<UPawn>();
    }

    protected override void BeginPlay()
    {
        base.BeginPlay();
        CanEverTick = true;
    }

    protected override void TickComponent(float deltaTime)
    {
        base.TickComponent(deltaTime);
    }
}
```

Checklist:

- Use `GetOwner<T>()` to cache the owning object.
- Use `Activate()` and `Deactivate()` for behavior state.
- Use `CanEverTick = true` only when needed.
- Keep server simulation authoritative when networked.

---

## Creating a New Game Mode

Derive from `UGameMode` when a scene needs custom match or possession rules.

```csharp
using Framework;
using Unity.Netcode;
using UnityEngine;

public class MyGameMode : UGameMode
{
    protected override void OnStartingNewPlayer(UController controller)
    {
        base.OnStartingNewPlayer(controller);
        Transform startSpot = ChoosePlayerStart(controller);

        if (startSpot != null)
        {
            controller.transform.SetPositionAndRotation(startSpot.position, startSpot.rotation);
        }
    }

    protected override UPawn ResolvePawnForController(UController controller, Transform startSpot)
    {
        return controller.GetComponentInChildren<UPawn>();
    }

    protected override UController ResolveControllerFromNetworkObject(NetworkObject networkObject)
    {
        return networkObject != null ? networkObject.GetComponent<UController>() : null;
    }
}
```

Checklist:

- Do not put `NetworkObject` on the game mode.
- Do not put game mode on the same GameObject as `NetworkManager` if you later make it a networked behavior.
- Let `NetworkManager` spawn player objects.
- Feed `NetworkClient.PlayerObject` into `HandleNetworkPlayerObject()`.
- Override `ResolvePawnForController()` to bind controllers to existing pawns.

---

## Scene Setup Checklist

- Add one scene `GameMode` object with a concrete `UGameMode`.
- Assign `DefaultControllerPrefab` to a prefab with `NetworkObject` and `UController`.
- Provide one or more `playerStarts`.
- Place or create a `NetworkManager`.
- Ensure `NetworkManager` has a `UnityTransport`.
- Ensure player pawns are either:
  - children of player controller objects, or
  - existing spawned scene/network objects that `ResolvePawnForController()` can find.
- Do not add `NetworkObject` to the `GameMode` object.

---

## Authority Rules

| Situation | Rule |
| --- | --- |
| Match start/end | Server or non-networked authority only. |
| Controller registration | Server authority when networked. |
| Possession | Server authoritative; clients request through RPC. |
| Movement simulation | Server authoritative when networked. |
| Input collection | Local owner only. |
| Camera/audio | Local pawn only. |

---

## Common Pitfalls

### `NetworkManager` and `NetworkObject` on the same GameObject

Do not do this. `NetworkManager` must not share a GameObject with `NetworkObject`.

Use this structure instead:

```text
GameMode
  FrameworkTestGameMode

NetworkManager
  NetworkManager
  UnityTransport
```

### Game Mode Spawning Player Objects

The current design expects `NetworkManager` to create player controller objects. Game mode should react to `OnClientConnectedCallback` and process the resulting `NetworkClient.PlayerObject`.

### No Available Pawn

If `FrameworkTestGameMode` logs that it cannot find an available pawn:

- Confirm the scene has a `UPawn`.
- Confirm the pawn is unpossessed.
- Confirm the pawn is spawned if networking is active.
- Confirm `ResolvePawnForController()` matches your desired pawn ownership model.

### Input Running on Remote Players

Player controller input should only run for local owners. `FrameworkTestPlayerController` already exits early when spawned and not owner.

---

## Current Extension Direction

The framework is currently best suited for:

- NetworkManager-driven player controller spawning.
- Server-authoritative possession and movement.
- Scene-owned or externally spawned pawns.
- First-person controller/pawn experiments.

If a game needs per-player pawn spawning, add a dedicated spawn system that works with `NetworkManager`, then let `UGameMode` consume those spawned objects instead of instantiating them directly.
