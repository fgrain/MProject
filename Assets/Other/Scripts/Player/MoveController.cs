using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MoveController : MonoBehaviour
{
    #region 属性

    public class MoveLevel
    {
        public float CalculatedAcceleration;
        public float MinRange = 0;
        public float MaxRange = Mathf.Infinity;
        public MovementState MoveState;
        public string Description;
    }

    public enum MovementState
    {
        Slow,
        Normal,
        Fast
    }

    public enum Kinestate
    {
        OnGround,
        OnStairs,
        OnClimb
    }

    private Rigidbody m_body;
    [SerializeField]
    Transform playerInputSpace = default;

    [Header("InitialParameter")]
    public float InitialMass;

    public float InitialMovementForce;
    public float RotateSpeed = 360;
    public List<MoveLevel> MoveLevels = new List<MoveLevel>();
    public int MaxAirJump = 2;
    public float JumpTime = 0.5f;
    public Vector3 Gravity = new Vector3(0, -9.8f, 0);
    
    //public float MaxJumpAccelerationTime = 0.2f;
    [Range(0f, 10f)]
    public float JumpAccelerationCoefficient = 3;

    [Range(0f, 1f)]
    public float JumpVelocityCoefficient = 0.5f, AirAccelerationCoefficient = 0.5f;

    //Kinematics Parameter
    private float m_MaxVelocity;

    private float m_MovementForce;
    private float m_Mass;
    private float m_Acceleration;
    private float m_MoveAcceleration;
    private float m_AirAcceleration;
    private float m_JumpAcceleration;
    private Vector3 m_InputMovement;
    private Vector3 m_Velocity, m_DesiredVelocity;
    private Vector3 m_ContactNormal, m_SteepNormal, m_ClimbNormal;

    //Ground Friction
    private float m_Friction;

    //Movement State Parameter
    private MovementState m_MovementState;

    private Kinestate m_Kinestate;
    private bool m_OnGround => m_GroundContactCount > 0;
    private bool m_OnSteep => m_SteepContactCount > 0;
    private bool m_OnClimb => m_ClimbContactCount > 0;
    private bool m_DesiredMove, m_StopMove;
    private bool m_DesiredJump, m_JumpPerform;
    private float m_JumpPerformTime = 0;

    private int m_StepsSinceLastJump, m_StepsSinceLastGrounded;
    private int m_JumpPhase;

    [SerializeField, Range(0, 140)]
    private float maxGroundAngle = 10f, maxSteepAngle = 60f, maxClimbAngle = 90f;

    private int m_GroundContactCount, m_SteepContactCount, m_ClimbContactCount;
    private float minGroundDotProduct, minSteepDotProduct, minClimbDotProduct;

    #endregion 属性

    #region 更新

    private void OnValidate()
    {
        ResetForceAndMass();
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minSteepDotProduct = Mathf.Cos(maxSteepAngle * Mathf.Deg2Rad);
        minClimbDotProduct = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
    }

    private void Awake()
    {
        m_body = GetComponent<Rigidbody>();
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minSteepDotProduct = Mathf.Cos(maxSteepAngle * Mathf.Deg2Rad);
        minClimbDotProduct = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);

        ResetMovementForce();
        ResetMass();
    }

    private void Update()
    {
        if (playerInputSpace)
        {
            Vector3 forward = playerInputSpace.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = playerInputSpace.right;
            right.y = 0f;
            right.Normalize();
            m_DesiredVelocity =
                (forward * m_InputMovement.z + right * m_InputMovement.x) * m_MaxVelocity;
        }
        else
        {
            m_DesiredVelocity = m_InputMovement * m_MaxVelocity;
        }
    }

    private void FixedUpdate()
    {
        UpdateState();

        if (m_DesiredMove)
        {
            if (m_StopMove)
            {
                m_DesiredMove = false;
                m_Velocity = new Vector3(0, m_Velocity.y, 0);
                if (m_OnGround || SnapToGround())
                    m_Velocity.y = 0;
            }
            else
            {
                AdjustVelocity();
            }
        }
        else
        {
            if (m_OnSteep)
            {
                ForceStaticMove(Gravity);
            }
        }

        if (m_DesiredJump)
        {
            m_DesiredJump = false;
            JumpStart();
        }
        if (m_JumpPerform && m_JumpPerformTime < JumpTime && m_JumpPhase < MaxAirJump)
        {
            m_Velocity.y = m_MaxVelocity * JumpVelocityCoefficient;
            m_JumpPerformTime += Time.deltaTime;
        }

        ClearState();
    }

    private void UpdateState()
    {
        m_Velocity = m_body.velocity;

        m_StepsSinceLastGrounded += 1;
        m_StepsSinceLastJump += 1;
        if (m_OnGround || CheckOnSteep() || SnapToGround())
        {
            m_StepsSinceLastGrounded = 0;
            if (m_StepsSinceLastJump > 1)
            {
                m_JumpPhase = 0;
            }
            if (m_GroundContactCount > 1)
            {
                m_ContactNormal.Normalize();
            }
        }
        else
        {
            m_ContactNormal = Vector3.up;
        }
    }

    private void ClearState()
    {
        m_body.velocity = m_Velocity;
        m_GroundContactCount = m_SteepContactCount = m_ClimbContactCount = 0;
        m_ContactNormal = m_SteepNormal = m_ClimbNormal = Vector3.zero;
    }

    public void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        float currentX = Vector3.Dot(m_Velocity, xAxis);
        float currentZ = Vector3.Dot(m_Velocity, zAxis);

        float acceleration = m_OnGround || m_OnSteep ? m_MoveAcceleration : m_AirAcceleration;
        float deltaSpeed = acceleration * Time.fixedDeltaTime;

        float newX = Mathf.MoveTowards(currentX, m_DesiredVelocity.x, deltaSpeed);
        float newZ = Mathf.MoveTowards(currentZ, m_DesiredVelocity.z, deltaSpeed);

        m_Velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
        //PlayerRotation(m_Velocity);
        //if (m_OnClimb)
        //{
        //    Vector3 velocityDir = new Vector3(-m_ClimbNormal.z, 0, m_ClimbNormal.x).normalized;
        //    float dotV = Vector3.Dot(m_Velocity, velocityDir);
        //    if (dotV < -0.8)
        //    {
        //        m_Velocity = dotV * velocityDir;
        //    }
        //}
    }

    private void PlayerRotation(Vector3 destVelocity)
    {
        Quaternion r = m_body.rotation;

        if (destVelocity != Vector3.zero)
        {
            Quaternion destRotate = Quaternion.LookRotation(destVelocity);
            r = Quaternion.RotateTowards(r, destRotate, RotateSpeed * Time.deltaTime);
            m_body.MoveRotation(r);
        }
    }

    private void JumpStart()
    {
        Vector3 jumpDirection;

        if (m_OnGround || m_OnSteep)
        {
            jumpDirection = Vector3.up;
        }
        else if (m_OnClimb)
        {
            jumpDirection = m_ClimbNormal.normalized + Vector3.up;
            m_JumpPhase = 0;
        }
        else if (MaxAirJump > 0 && m_JumpPhase <= MaxAirJump)
        {
            if (m_JumpPhase == 0)
            {
                m_JumpPhase = 1;
            }
            jumpDirection = Vector3.up;
        }
        else
        {
            return;
        }

        if (m_JumpPhase > 0)
        {
            m_Velocity = Vector3.zero;
        }

        m_StepsSinceLastJump = 0;
        m_JumpPhase += 1;
        float jumpSpeed = m_MaxVelocity * JumpVelocityCoefficient;
        if (m_Velocity.y < 0)
        {
            m_Velocity.y = 0;
        }
        m_Velocity += jumpSpeed * jumpDirection;
    }

    private void ForceStaticMove(Vector3 force)
    {
        float sinG = ProjectOnContactPlane(force).magnitude * m_Mass;
        float cosG = ProjectOnContactPlane(force).magnitude * m_Friction * m_Mass;
        if (sinG <= cosG)
        {
            m_Velocity = Vector3.zero;
        }
    }

    #endregion 更新

    #region 属性计算

    /// <summary>
    /// 修改移动施加的力
    /// </summary>
    /// <param name="force"></param>
    public void ModifyMovementForce(float force)
    {
        m_MovementForce += force;
        SetAcceleration();
    }

    /// <summary>
    /// 重置移动施加的力
    /// </summary>
    public void ResetMovementForce()
    {
        m_MovementForce = InitialMovementForce;
        SetAcceleration();
    }

    /// <summary>
    /// 修改角色质量
    /// </summary>
    /// <param name="mass"></param>
    public void ModifyMass(float mass)
    {
        m_Mass += mass;
        SetAcceleration();
    }

    /// <summary>
    /// 重置角色质量
    /// </summary>
    public void ResetMass()
    {
        m_Mass = InitialMass;
        SetAcceleration();
    }

    /// <summary>
    /// 重置力和质量
    /// </summary>
    public void ResetForceAndMass()
    {
        m_MovementForce = InitialMovementForce;
        m_Mass = InitialMass;
        SetAcceleration();
    }

    /// <summary>
    /// 设置加速度
    /// </summary>
    private void SetAcceleration()
    {
        float acceleration = m_MovementForce / m_Mass;
#if UNITY_EDITOR
        m_Acceleration = acceleration;
        m_MaxVelocity = m_MovementForce / m_Mass;
#endif
        foreach (var level in MoveLevels)
        {
            if (acceleration >= level.MinRange && acceleration <= level.MaxRange)
            {
                m_Acceleration = level.CalculatedAcceleration;
                m_MovementState = level.MoveState;
                switch (m_MovementState)
                {
                    case MovementState.Slow:
                        m_MaxVelocity = m_MovementForce / (2 * m_Mass);
                        break;

                    case MovementState.Normal:
                        m_MaxVelocity = m_MovementForce / m_Mass;
                        break;

                    case MovementState.Fast:
                        m_MaxVelocity = m_MovementForce * 2 / m_Mass;
                        break;

                    default:
                        Debug.Log("未设置移动速度上限");
                        break;
                }
                break;
            }
        }
        m_JumpAcceleration = JumpAccelerationCoefficient * m_Acceleration;
        m_AirAcceleration = AirAccelerationCoefficient * m_Acceleration;
    }

    #endregion 属性计算

    #region 状态检测

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            float upDot = Vector3.Dot(Vector3.up, normal);

            if (upDot >= minGroundDotProduct)
            {
                m_ContactNormal += normal;
                m_GroundContactCount += 1;
            }
            else if (upDot >= minSteepDotProduct)
            {
                m_SteepNormal += normal;
                m_SteepContactCount += 1;
            }
            else if (upDot >= minClimbDotProduct)
            {
                m_ClimbNormal += normal;
                m_ClimbContactCount += 1;
            }

            if (collision.transform.TryGetComponent(out PlatformFriction platform))
            {
                m_Friction = platform.Friction;
                m_MoveAcceleration = m_Acceleration * m_Friction;
            }
            else
            {
                m_Friction = 1;
                m_MoveAcceleration = m_Acceleration;
            }
        }
    }

    private bool SnapToGround()
    {
        if (m_StepsSinceLastGrounded > 1 || m_StepsSinceLastJump <= 2)
        {
            return false;
        }
        if (!Physics.Raycast(m_body.position, Vector3.down, out RaycastHit hit))
        {
            return false;
        }
        if (hit.normal.y < minSteepDotProduct)
        {
            return false;
        }

        m_GroundContactCount = 1;
        m_ContactNormal = hit.normal;
        float speed = m_Velocity.magnitude;
        float dot = Vector3.Dot(m_Velocity, hit.normal);
        if (dot > 0f)
        {
            m_Velocity = (m_Velocity - hit.normal * dot).normalized * speed;
            Debug.Log(m_Velocity);
        }

        return true;
    }

    private bool CheckOnSteep()
    {
        if (m_OnSteep)
        {
            m_SteepNormal.Normalize();
            m_ContactNormal = m_SteepNormal;
            return true;
        }
        return false;
    }

    private Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - m_ContactNormal * Vector3.Dot(vector, m_ContactNormal);
    }

    #endregion 状态检测

    #region 调用接口

    public void DesiredMove(Vector3 inputMovement)
    {
        m_DesiredMove = true;
        m_StopMove = false;

        m_InputMovement = inputMovement;
    }

    public void StopMove()
    {
        m_StopMove = true;
    }

    public void DesiredJump(bool desiredJump)
    {
        m_DesiredJump |= desiredJump;
    }

    public void JumpPerform()
    {
        m_JumpPerform = true;
        m_JumpPerformTime = 0;
    }

    public void JumpStop()
    {
        m_JumpPerform = false;
    }

    #endregion 调用接口
}