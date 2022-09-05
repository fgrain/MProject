using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SimpleMove : MonoBehaviour
{
    public static PlayerInputAction PlayerInputAction;
    private Rigidbody body;
    private bool desireMove;
    private Vector3 inputMovement;
    public float speed = 1;

    private void Awake()
    {
        InitInputSystem();
        body = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (desireMove)
        {
            body.velocity += speed * inputMovement * Time.deltaTime;
        }
    }

    private void InitInputSystem()
    {
        if (PlayerInputAction == null)
            PlayerInputAction = new PlayerInputAction();
        PlayerInputAction.Enable();
        PlayerInputAction.Player.Move.performed += Move_performed;
        PlayerInputAction.Player.Move.canceled += Move_canceled;
    }

    private void Move_canceled(InputAction.CallbackContext obj)
    {
        desireMove = false;
    }

    private void Move_performed(InputAction.CallbackContext obj)
    {
        Vector2 input = obj.ReadValue<Vector2>();
        inputMovement = new Vector3(input.x, 0, input.y);
        desireMove = true;
    }
}