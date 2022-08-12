using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MoveController : MonoBehaviour
{
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

    [Header("InitialParameter")]
    public float InitialMass;

    public float InitialMovementForce;
    public List<MoveLevel> MoveLevels = new List<MoveLevel>();
    public int MaxAirJump = 2;

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
    private float m_GroundAcceleration;
    private float m_AirAcceleration;
    private float m_JumpAcceleration;
    private Vector3 m_velocity, m_desiredVelocity;
    private Vector3 m_contactNormal;

    //Ground Friction
    private float m_Friction;

    //Movement State Parameter
    private MovementState m_MovementState;

    private Kinestate m_Kinestate;
    private bool onGround => groundContactCount > 0;
    private bool m_DesiredMove;

    //Jump State
    private bool m_DesiredJump;

    private int m_StepsSinceLastJump;
    private int m_JumpPhase;
    //private float m_JumpAccelerationTime;

    [SerializeField, Range(0, 140)]
    private float maxGroundAngle = 10f, maxStairsAngle = 60f, maxClimbAngle = 90f;

    private int groundContactCount;
    private float minGroundDotProduct, minStairsDotProduct, minClimbDotProduct;

    private void OnValidate()
    {
        ResetForceAndMass();
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
        minClimbDotProduct = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
    }

    private void Awake()
    {
        m_body = GetComponent<Rigidbody>();
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
        minClimbDotProduct = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);

        ResetMovementForce();
        ResetMass();
    }

    private void FixedUpdate()
    {
        UpdateState();
        if (m_DesiredMove)
        {
            AdjustVelocity();
        }
        else
        {
            m_velocity = new Vector3(0, m_body.velocity.y, 0);
            m_body.velocity = m_velocity;
        }

        if (m_DesiredJump)
        {
            m_DesiredJump = false;
            Jump();
        }
        ClearState();
    }

    private void UpdateState()
    {
        m_velocity = m_body.velocity;

        m_StepsSinceLastJump += 1;
        if (onGround)
        {
            if (m_StepsSinceLastJump > 1)
            {
                m_JumpPhase = 0;
            }
            if (groundContactCount > 1)
            {
                m_contactNormal.Normalize();
            }
        }
        else
        {
            m_contactNormal = Vector3.up;
        }
    }

    private void ClearState()
    {
        m_body.velocity = m_velocity;
        groundContactCount = 0;
        m_contactNormal = Vector3.zero;
    }

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

    public void DesiredMove(Vector3 inputMovement)
    {
        if (inputMovement != Vector3.zero)
        {
            m_DesiredMove = true;
            m_desiredVelocity = inputMovement * m_MaxVelocity;
        }
        else
        {
            m_DesiredMove = false;
        }
    }

    public void DesiredJump(bool desiredJump)
    {
        m_DesiredJump |= desiredJump;
    }

    public void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        float currentX = Vector3.Dot(m_velocity, xAxis);
        float currentZ = Vector3.Dot(m_velocity, zAxis);

        float acceleration = onGround ? m_GroundAcceleration : m_AirAcceleration;
        float deltaSpeed = acceleration * Time.deltaTime;

        float newX = Mathf.MoveTowards(currentX, m_desiredVelocity.x, deltaSpeed);
        float newZ = Mathf.MoveTowards(currentZ, m_desiredVelocity.z, deltaSpeed);

        m_velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    private void Jump()
    {
        if (onGround || m_JumpPhase < MaxAirJump)
        {
            m_StepsSinceLastJump = 0;
            m_JumpPhase += 1;
        }
        else
        {
            return;
        }

        float deltaSpeed = m_JumpAcceleration * Time.fixedDeltaTime;
        m_velocity.y = m_MaxVelocity * JumpVelocityCoefficient;
    }

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
                m_contactNormal += normal;
                groundContactCount += 1;
                if (collision.transform.TryGetComponent(out PlatformFriction platform))
                {
                    m_Friction = platform.Friction;
                    m_GroundAcceleration = m_Acceleration * m_Friction;
                }
                else
                {
                    m_GroundAcceleration = m_Acceleration;
                }
            }
        }
    }

    private Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - m_contactNormal * Vector3.Dot(vector, m_contactNormal);
    }
}