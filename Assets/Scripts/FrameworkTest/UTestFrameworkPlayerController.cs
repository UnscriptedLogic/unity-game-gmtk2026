using Framework;
using Framework.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FrameworkTest
{
    public class UTestFrameworkPlayerController : UController
    {
        [SerializeField] private InputActionAsset inputActionAsset;
        
        protected override void BeginPlay()
        {
            base.BeginPlay();
            
            inputActionAsset.Enable();
            inputActionAsset["Move"].performed += OnMovePerformed;
            inputActionAsset["Move"].canceled += OnMovePerformed;
            
            inputActionAsset["Jump"].performed += OnJumpPerformed;
        }

        private void OnJumpPerformed(InputAction.CallbackContext obj)
        {
            if (HasPawn)
            {
                Pawn.TryGetComponent(out CharacterMovementComponent component);
                component.Jump();
            }
        }

        private void OnMovePerformed(InputAction.CallbackContext obj)
        {
            Vector2 moveInput = obj.ReadValue<Vector2>();
            
            //2D movement only
            moveInput.y = 0f;

            if (HasPawn)
            {
                if (Pawn.TryGetComponent(out CharacterMovementComponent component))
                {
                    component.SetMovementInput(moveInput);
                }
            }
        }
    }
}