using Framework;
using Framework.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FrameworkTest
{
    public class UTestFrameworkPlayerController : UController
    {
        [SerializeField] private InputActionAsset inputActionAsset;
        
        private UTestFrameworkPlayerPawn _playerPawn;
        
        protected override void BeginPlay()
        {
            base.BeginPlay();
            
            inputActionAsset.Enable();
            inputActionAsset["Move"].performed += OnMovePerformed;
            inputActionAsset["Move"].canceled += OnMovePerformed;
            
            inputActionAsset["Jump"].performed += OnJumpPerformed;

            inputActionAsset["Interact"].performed += OnInteractPerformed;
            inputActionAsset["ArmBomb"].performed += OnArmBombPerformed;
        }

        private void OnArmBombPerformed(InputAction.CallbackContext obj)
        {
            if (_playerPawn)
            {
                _playerPawn.OnArmBomb();
            }
        }

        protected override void OnPossess(UPawn pawn)
        {
            base.OnPossess(pawn);
            
            _playerPawn = pawn as UTestFrameworkPlayerPawn;
            _playerPawn.BombArmTimer.OnValueChanged += OnBombTimerChanged;
        }

        private void OnBombTimerChanged(float previousValue, float newValue)
        {
            if (newValue <= 0.1f)
            {
                if (!IsServer) return;
                
                _playerPawn.DoKnockbackServerRpc();
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext obj)
        {
            if (_playerPawn)
            {

            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext obj)
        {
            if (_playerPawn)
            {
                _playerPawn.OnJump();
            }
        }

        private void OnMovePerformed(InputAction.CallbackContext obj)
        {
            Vector2 moveInput = obj.ReadValue<Vector2>();
            
            moveInput.y = 0f;

            if (_playerPawn)
            {
                _playerPawn.OnMove(moveInput);
            }
        }
    }
}