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
        }

        private void OnMovePerformed(InputAction.CallbackContext obj)
        {
            Vector2 moveInput = obj.ReadValue<Vector2>();

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