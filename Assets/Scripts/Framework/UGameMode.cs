using Unity.Netcode;
using UnityEngine;

namespace Framework
{
    public abstract class UGameMode : NetworkBehaviour
    {
        protected NetworkManager networkManager;
        protected UGameState gameState;

        protected virtual void Awake()
        {
            RegisterGameState();
            RegisterNetworkManager();
        }

        public override void OnDestroy()
        {
            UnregisterNetworkManager();
            base.OnDestroy();
        }

        protected virtual void RegisterGameState()
        {
            gameState = UGameState.Instance != null
                ? UGameState.Instance
                : FindAnyObjectByType<UGameState>();

            if (gameState == null)
            {
                Debug.LogError($"{nameof(UGameMode)} requires a {nameof(UGameState)} in the scene to track connected players.");
            }
        }

        private void RegisterNetworkManager()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkManager is not present in the scene. Please add a NetworkManager prefab to the scene.");
                return;
            }
            
            networkManager = NetworkManager.Singleton;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;

            if (networkManager.IsServer)
            {
                RegisterConnectedClients();
            }
        }

        private void UnregisterNetworkManager()
        {
            if (networkManager == null)
            {
                return;
            }

            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            networkManager = null;
        }

        protected virtual void OnClientDisconnected(ulong clientId)
        {
            if (!IsServer || gameState == null)
            {
                return;
            }

            gameState.RemoveConnectedPlayer(clientId);
        }

        protected virtual void OnClientConnected(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            RegisterConnectedClient(clientId);
        }

        protected virtual void RegisterConnectedClients()
        {
            if (networkManager == null)
            {
                return;
            }

            foreach (NetworkClient client in networkManager.ConnectedClientsList)
            {
                RegisterConnectedClient(client.ClientId);
            }
        }

        protected virtual bool RegisterConnectedClient(ulong clientId)
        {
            if (gameState == null)
            {
                RegisterGameState();
            }

            if (networkManager == null || gameState == null)
            {
                return false;
            }

            if (!networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
            {
                return false;
            }

            NetworkObject playerObject = client.PlayerObject;

            if (playerObject == null)
            {
                Debug.LogWarning($"Client {clientId} connected without an associated player prefab instance.");
                return false;
            }

            gameState.AddOrUpdateConnectedPlayer(clientId, playerObject);
            return true;
        }
    }
}
