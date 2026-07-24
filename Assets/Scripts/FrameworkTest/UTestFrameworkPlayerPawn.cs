using Framework;
using Framework.Components;
using Framework.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace FrameworkTest
{
    public class UTestFrameworkPlayerPawn : UPawn
    {
        [SerializeField] private float bombArmTime = 10f;
        [SerializeField] private PlayerWidget playerWidgetPrefab;
        
        private NetworkVariable<bool> _isBombActive = new();
        private NetworkVariable<float> _bombTimer = new ();
        
        private KnockbackBombComponent _knockbackBombComponent;
        private CharacterMovementComponent _characterMovementComponent;
        private InteractionComponent _interactionComponent;

        private PlayerWidget _playerWidgetInstance;

        public NetworkVariable<float> BombArmTimer => _bombTimer;
        
        protected override void BeginPlay()
        {
            base.BeginPlay();
            
            _playerWidgetInstance = Instantiate(playerWidgetPrefab, transform);
            _playerWidgetInstance.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            _playerWidgetInstance.Initialize(ref _bombTimer);
        }

        protected override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (!IsServer) return;
            
            UpdateBombTimer(deltaTime);
        }

        #region Bomb Arming
        
        private void UpdateBombTimer(float deltaTime)
        {
            if (_isBombActive.Value)
            {
                _bombTimer.Value -= deltaTime;

                if (_bombTimer.Value <= 0)
                {
                    _bombTimer.Value = 0f;
                }   
            }
        }
        
        public void OnArmBomb()
        {
            if (!IsOwner) return;
            if (_isBombActive.Value) return;
            
            StartArmTimerServerRpc();
        }

        [ServerRpc]
        private void StartArmTimerServerRpc()
        {
            if (!_isBombActive.Value)
            {
                _bombTimer.Value = bombArmTime;
                _isBombActive.Value = true;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Server)]
        public void DoKnockbackServerRpc()
        {
            if (_isBombActive.Value && _bombTimer.Value <= 0.1f)
            {
                if (_knockbackBombComponent == null)
                {
                    TryGetComponent(out _knockbackBombComponent);
                }
                
                if (_knockbackBombComponent != null)
                {
                    _knockbackBombComponent.DoKnockback();
                }
                
                _isBombActive.Value = false;
            }
        }
        
        #endregion

        public void OnInteract()
        {
            if (_interactionComponent == null)
            {
                TryGetComponent(out _interactionComponent);
            }
            
            if (_interactionComponent != null)
            {
                if (_interactionComponent.HasAnyButtons)
                {
                    _interactionComponent.FirstButton.Interact();
                }
            }
        }
        
        public void OnJump()
        {
            if (_characterMovementComponent == null)
            {
                TryGetComponent(out _characterMovementComponent);
            }

            if (_characterMovementComponent != null)
            {
                _characterMovementComponent.Jump();
            }
        }

        public void OnMove(Vector2 moveInput)
        {
            if (_characterMovementComponent == null)
            {
                TryGetComponent(out _characterMovementComponent);
            }

            if (_characterMovementComponent != null)
            {
                _characterMovementComponent.SetMovementInput(moveInput);
            }
        }
    }
}