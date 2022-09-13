using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        private G2Move m_PlayerMoveController;
        private Vector3 m_InputMovement;

        private void Awake()
        {
            InitInputSystem();
            m_PlayerMoveController = GetComponent<G2Move>();
        }

        private void InitInputSystem()
        {
            KInput.Enable();
            KInput.Player.Move.performed += OnMovement;
            KInput.Player.Move.canceled += OnMovementStop;

            KInput.Player.Jump.started += OnJumpStart;
            KInput.Player.Jump.performed += OnJumpPerform;
            KInput.Player.Jump.canceled += OnJumpCancel;
        }

        private void OnJumpCancel(InputAction.CallbackContext obj)
        {
            m_PlayerMoveController.JumpInputStop();
        }

        private void OnJumpPerform(InputAction.CallbackContext obj)
        {
            m_PlayerMoveController.JumpPerform();
        }

        private void OnMovementStop(InputAction.CallbackContext value)
        {
            m_PlayerMoveController.StopMove();
        }

        public void OnMovement(InputAction.CallbackContext value)
        {
            Vector2 inputMovement = value.ReadValue<Vector2>();
            m_InputMovement = new Vector3(inputMovement.x, 0, inputMovement.y);
            m_PlayerMoveController.DesiredMove(m_InputMovement);
        }

        public void OnJumpStart(InputAction.CallbackContext value)
        {
            m_PlayerMoveController.DesiredJump(true);
        }
    }

    public static class KInput
    {
        static KInput()
        {
            Player.Enable();
            CurrentMap = Player.Get();
        }

        public static void ChangeCurrentMap(InputActionMap map)
        {
            CurrentMap.Disable();
            CurrentMap = map;
            CurrentMap.Enable();
        }

        #region InputAction

        public static readonly PlayerInputAction InputAction = new();

        public static void Enable() => InputAction.Enable();

        public static void Disable() => InputAction.Disable();

        public static InputActionMap CurrentMap;

        public static PlayerInputAction.PlayerActions Player => InputAction.Player;

        public static PlayerInputAction.UIActions UI => InputAction.UI;

        #endregion InputAction
    }
}