using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[SaveDuringPlay]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 lockOffset;
    [SerializeField] private Vector2 clampAxis = new Vector2(60, 60);

    [SerializeField] private float follow_smoothing = 5;
    [SerializeField] private float rotate_Smoothing = 5;
    [SerializeField] private float senstivity = 60;

    private float rotX, rotY;
    private bool cursorLocked = false;
    private Transform cam;
    
    private PlayerInputAction m_PlayerInput;
    [HideInInspector] public bool lockedTarget;
    [HideInInspector] public Transform LockTarget;

    private void Awake()
    {
        m_PlayerInput = new PlayerInputAction();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        cam = Camera.main.transform;
    }

    private void OnEnable()
    {
        m_PlayerInput.Enable();
    }

    private void OnDisable()
    {
        m_PlayerInput.Disable();
    }

    private void Update()
    {
        Vector3 target_P = transform.InverseTransformVector(target.position + offset) + lockOffset;
        Vector3 localPosition = Vector3.Lerp(transform.localPosition, target_P, follow_smoothing * Time.deltaTime);
        transform.position = transform.TransformVector(localPosition);
        //transform.position = target_P;
        if (!lockedTarget) CameraTargetRotation(); else LookAtTarget();

        if (Keyboard.current.altKey.wasPressedThisFrame)
        {
            cursorLocked = !cursorLocked;
            CursorState();
        }
    }

    private void CameraTargetRotation()
    {
        Vector2 mouseAxis = m_PlayerInput.Player.Look.ReadValue<Vector2>();
        rotX += (mouseAxis.x * senstivity) * Time.deltaTime;
        rotY -= (mouseAxis.y * senstivity) * Time.deltaTime;

        rotY = Mathf.Clamp(rotY, clampAxis.x, clampAxis.y);

        Quaternion localRotation = Quaternion.Euler(rotY, rotX, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, localRotation, Time.deltaTime * rotate_Smoothing);
    }

    private void LookAtTarget()
    {
        //transform.rotation = cam.rotation;
        //Vector3 r = cam.eulerAngles;
        //rotX = r.y;
        //rotY = 1.8f;
        if (!LockTarget) return;
        Vector3 diection = (transform.position - LockTarget.position).normalized;
        offset = diection * 3;
        transform.rotation = cam.rotation;
    }

    private void CursorState()
    {
        if (cursorLocked)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}