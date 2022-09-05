using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private G2Move m_PlayerMoveController;
    private Vector3 m_InputMovement;
    public static PlayerInputAction PlayerInputAction;

    private void Awake()
    {
        InitInputSystem();
        m_PlayerMoveController = GetComponent<G2Move>();
    }

    private void InitInputSystem()
    {
        if (PlayerInputAction == null)
            PlayerInputAction = new PlayerInputAction();
        PlayerInputAction.Enable();
        PlayerInputAction.Player.Move.performed += OnMovement;
        PlayerInputAction.Player.Move.canceled += OnMovementStop;
        
        PlayerInputAction.Player.Jump.started += OnJumpStart;
        PlayerInputAction.Player.Jump.performed += OnJumpPerform;
        PlayerInputAction.Player.Jump.canceled += OnJumpCancel;
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