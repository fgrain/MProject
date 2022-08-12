using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MoveController))]
public class PlayerController : MonoBehaviour
{
    private MoveController PlayerMoveController;
    private Vector3 m_InputMovement;

    private void Start()
    {
        PlayerMoveController = GetComponent<MoveController>();
    }

    public void OnMovement(InputAction.CallbackContext value)
    {
        Vector2 inputMovement = value.ReadValue<Vector2>();
        m_InputMovement = new Vector3(inputMovement.x, 0, inputMovement.y);
        PlayerMoveController.DesiredMove(m_InputMovement);
    }

    public void OnJump(InputAction.CallbackContext value)
    {
        if (value.started)
        {
            PlayerMoveController.DesiredJump(true);
        }
        else if (value.performed)
        {
            PlayerMoveController.DesiredJump(true);
        }
        else if (value.canceled)
        {
            PlayerMoveController.DesiredJump(false);
        }
    }
}