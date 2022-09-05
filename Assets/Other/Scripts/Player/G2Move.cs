using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class G2Move : MonoBehaviour
{
    #region 属性

    public Rigidbody Body { get; private set; }

    private CapsuleCollider m_Collider;

    private Transform playerInputSpace = default;

    [Header("InitialParameter")]
    public float InitialMass = 1;

    public float InitialMovementForce = 10;
    public Vector3 Gravity = new Vector3(0, -9.8f, 0);
    public float AnimationSmooth = 0.2f;
    public LayerMask GroundLayer = 1 << 0;
    [SerializeField, Range(0, 140)] private float maxGroundAngle = 60f;

    [Header("Movement")]
    public float GroundDistance = 1.5f;

    public float StepHeight = 0.4f;
    public float StepSmooth = 5f;
    [Range(0f, 1f)] public float AirAccelerationCoefficient = 0.5f;
    [Range(1f, 3f)] public float SprintCoefficient = 2;
    [Range(0f, 1f)] public float WalkCoefficient = 0.5f;
    private float groundMinDistance = 0.4f;
    private float groundMaxDistance = 1.5f;
    private float groundDistance;

    [Header("Jump and Falling")]
    public int MaxAirJump = 1;

    public float JumpTime = 0.5f;
    [Range(0f, 3f)] public float JumpVelocityCoefficient = 0.5f;
    [Range(0f, 2f)] public float AirJumpVelocityCoefficient = 1;
    [Range(0f, 1f)] public float FallOffsetCoefficient = 0.5f;
    [Range(0f, 5f)] public float WallJumpCoefficient = 2;

    [Header("Rotation")]
    public float RotateSpeed = 360;

    public float AirRotateSpeed = 720;

    private float m_MovementForce;
    private float m_Mass;
    private float m_MaxVelocity;
    private float m_Acceleration;
    private float m_MoveAcceleration;
    private float m_AirAcceleration;
    private float m_Sprint;
    private float m_Walk;
    private float m_Friction;
    private int m_JumpPhase;
    private Vector3 m_PreInput;
    private Vector3 m_InputMovement;
    private Vector3 m_Velocity, m_DesiredVelocity;
    private Vector3 m_ContactNormal, m_SteepNormal, m_ClimbNormal;

    private bool m_OnGround => m_GroundContactCount > 0;
    private bool m_OnSteep => m_SteepContactCount > 0;
    private bool m_OnJumpDown => m_OnAir && Body.velocity.y < 0;
    private bool m_OnSnapGround;
    private bool m_OnAir;
    private bool m_OnWalk;
    private bool m_DesiredMove, m_StopMove;
    private bool m_DesiredJump, m_JumpPerform;

    private float m_JumpPerformTime = 0;
    private int m_StepsSinceLastJump, m_StepsSinceLastGrounded;
    private RaycastHit groundHit;

    private int m_GroundContactCount, m_SteepContactCount;
    private float minGroundDotProduct;

    #endregion 属性

    #region 更新

    private void OnValidate()
    {
        ResetForceAndMass();
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    private void Awake()
    {
        Body = GetComponent<Rigidbody>();
        m_Collider = GetComponent<CapsuleCollider>();

        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);

        playerInputSpace = Camera.main.transform;

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
            m_DesiredVelocity = (forward * m_InputMovement.z + right * m_InputMovement.x) * m_MaxVelocity * m_Sprint * m_Walk;
            ProcessRotation();
        }
        else
        {
            m_DesiredVelocity = m_InputMovement * m_MaxVelocity * m_Sprint;
        }
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        UpdateState();

        PlayerMovement();

        ClearState();
        CheckGroundDistance();
    }

    private void UpdateState()
    {
        StepClimb();
        m_Velocity = Body.velocity;
        m_StepsSinceLastGrounded += 1;
        m_StepsSinceLastJump += 1;
        if (m_OnGround || SnapToGround())
        {
            m_OnAir = false;
            m_StepsSinceLastGrounded = 0;
            if (m_OnGround && m_StepsSinceLastJump > 1)
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
            if (groundDistance > groundMaxDistance)
                m_OnAir = true;
            m_OnSnapGround = false;
            m_ContactNormal = Vector3.up;
        }
    }

    private void ClearState()
    {
        Body.velocity = m_Velocity;
        m_GroundContactCount = m_SteepContactCount = 0;
        m_ContactNormal = m_SteepNormal = m_ClimbNormal = Vector3.zero;
    }

    private void PlayerMovement()
    {
        HorizontalMove();

        if (ForceStaticMove(Gravity))
        {
            m_Velocity += Gravity * Time.fixedDeltaTime;
        }

        Jump();
    }

    public void HorizontalMove()
    {
        if (m_DesiredMove)
        {
            if (m_StopMove)
            {
                m_DesiredMove = false;
                m_Velocity = new Vector3(m_Velocity.x * FallOffsetCoefficient, m_Velocity.y, m_Velocity.z * FallOffsetCoefficient);

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
            if (m_OnGround)
            {
                m_Velocity = Vector3.zero;
            }
        }
    }

    public void Jump()
    {
        if (m_DesiredJump)
        {
            m_DesiredJump = false;
            JumpStart();
        }
        if (m_JumpPerform && m_JumpPerformTime < JumpTime && m_JumpPhase <= MaxAirJump)
        {
            m_JumpPerformTime += Time.fixedDeltaTime;
            m_Velocity.y = m_MaxVelocity * JumpVelocityCoefficient;
        }
    }

    public void AdjustVelocity()
    {
        if (m_OnSteep)
        {
            Vector3 normal = m_SteepNormal;
            Vector3 steepForward = new Vector3(normal.x, 0, normal.z).normalized;
            float steepDot = Vector3.Dot(steepForward, m_DesiredVelocity.normalized);
            if (steepDot < -0.8)
            {
                if (m_OnGround)
                    m_Velocity = Vector3.zero;
                return;
            }
        }

        Vector3 desiredMoveDir = Vector3.ProjectOnPlane(m_DesiredVelocity, m_ContactNormal).normalized;
        if (desiredMoveDir.y > 0 && ForceStaticMove(Gravity))
        {
            desiredMoveDir.y = 0;
            if (m_Velocity.y > 0)
                m_Velocity.y = 0;
        }

        if (m_OnGround && m_PreInput != m_InputMovement)
        {
            m_Velocity = desiredMoveDir * m_Velocity.magnitude;
        }
        if (m_Velocity.magnitude < m_DesiredVelocity.magnitude)
        {
            float acceleration = m_OnGround ? m_MoveAcceleration : m_AirAcceleration;
            float deltaSpeed = acceleration * Time.fixedDeltaTime;

            m_Velocity += desiredMoveDir * deltaSpeed;
        }
        else
        {
            m_Velocity.x = m_DesiredVelocity.x;
            m_Velocity.z = m_DesiredVelocity.z;
        }
    }

    private void ProcessRotation()
    {
        Quaternion r = Body.rotation;
        float rotateSpeed;
        if (m_OnAir)
        {
            rotateSpeed = AirRotateSpeed;
        }
        else
        {
            rotateSpeed = RotateSpeed * m_Velocity.magnitude;
        }

        if (m_DesiredVelocity != Vector3.zero)
        {
            Quaternion destRotate = Quaternion.LookRotation(m_DesiredVelocity);
            r = Quaternion.RotateTowards(r, destRotate, rotateSpeed * Time.deltaTime);
            Body.MoveRotation(r);
        }
        //else if (PatternAttacker && playerInputSpace && PatternAttacker.IsAttacking)
        //{
        //    float rotationY = playerInputSpace.rotation.eulerAngles.y;
        //    Quaternion destRotate = Quaternion.Euler(0, rotationY, 0);
        //    r = Quaternion.RotateTowards(r, destRotate, RotateSpeed * m_Velocity.magnitude * Time.deltaTime);
        //    Body.MoveRotation(r);
        //}
    }

    private void JumpStart()
    {
        Vector3 jumpDirection;

        if (m_OnGround)
        {
            jumpDirection = Vector3.up;
        }
        else if (m_OnSteep && m_JumpPhase <= MaxAirJump)
        {
            jumpDirection = m_ClimbNormal.normalized + Vector3.up * WallJumpCoefficient;
            jumpDirection.Normalize();
            m_Velocity.y = 0;
        }
        else if (MaxAirJump > 0 && m_JumpPhase <= MaxAirJump)
        {
            if (m_JumpPhase == 0)
            {
                m_JumpPhase = 1;
            }
            m_Velocity.y = 0;
            jumpDirection = Vector3.up;
        }
        else
        {
            return;
        }

        float airJumpVelocity = 1;
        if (m_JumpPhase > 0)
        {
            m_Velocity.y = 0;
            airJumpVelocity = AirJumpVelocityCoefficient;
        }
        //m_AnimationInput.SetPlayerJump(m_OnGround);
        m_StepsSinceLastJump = 0;
        m_JumpPhase += 1;
        float jumpSpeed = m_MaxVelocity * JumpVelocityCoefficient * airJumpVelocity;
        if (m_Velocity.y < 0)
        {
            m_Velocity.y = 0;
        }
        m_Velocity += jumpSpeed * jumpDirection;
    }

    private bool ForceStaticMove(Vector3 force)
    {
        if (m_OnSteep)
            return true;
        if (m_OnGround)
            return false;

        //if (m_OnSteep)
        //{
        //    //float angle = Vector3.Angle(m_ContactNormal, -force);
        //    //float sinG = Mathf.Sin(angle * Mathf.Deg2Rad) * force.magnitude;
        //    //float cosG = Mathf.Cos(angle * Mathf.Deg2Rad) * force.magnitude * m_Friction;
        //    //if (sinG >= cosG)
        //    //{
        //    //    m_Velocity.y = m_Velocity.y > 0 ? 0 : m_Velocity.y;
        //    //    return true;
        //    //}
        //    //else
        //    //    return false;
        //    return true;
        //}

        return true;
    }

    public void UpdateAnimator()
    {
        float speed = transform.InverseTransformDirection(m_DesiredVelocity).magnitude;
        //m_AnimationInput.SetJumpState(m_OnJumpDown, m_OnGround || m_OnSnapGround);
        //m_AnimationInput.SetPlayerLocomotion(speed, AnimationSmooth);
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
        m_Sprint = 1;
        m_Walk = 1;
        m_OnWalk = false;
#if UNITY_EDITOR
        m_Acceleration = acceleration;
        m_MaxVelocity = m_MovementForce / m_Mass;
#endif
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
            CheckContact(normal);
            m_Friction = 1;
            m_MoveAcceleration = m_Acceleration * m_Friction;
        }
    }

    private void CheckContact(Vector3 normal)
    {
        float upDot = Vector3.Dot(Vector3.up, normal);
        if (upDot >= minGroundDotProduct)
        {
            m_ContactNormal += normal;
            m_GroundContactCount += 1;
        }
        else
        {
            m_SteepNormal += normal;
            m_SteepContactCount += 1;
        }
    }

    protected virtual void CheckGroundDistance()
    {
        if (m_Collider != null)
        {
            // radius of the SphereCast
            float radius = m_Collider.radius * 0.9f;
            var dist = 10f;
            // ray for RayCast
            Ray ray2 = new Ray(transform.position + new Vector3(0, m_Collider.height / 2, 0), Vector3.down);
            // raycast for check the ground distance
            if (Physics.Raycast(ray2, out groundHit, (m_Collider.height / 2) + dist, GroundLayer) && !groundHit.collider.isTrigger)
                dist = transform.position.y - groundHit.point.y;
            // sphere cast around the base of the capsule to check the ground distance
            if (dist >= groundMinDistance)
            {
                Vector3 pos = transform.position + Vector3.up * (m_Collider.radius);
                Ray ray = new Ray(pos, -Vector3.up);
                if (Physics.SphereCast(ray, radius, out groundHit, m_Collider.radius + groundMaxDistance, GroundLayer) && !groundHit.collider.isTrigger)
                {
                    Physics.Linecast(groundHit.point + (Vector3.up * 0.1f), groundHit.point + Vector3.down * 0.15f, out groundHit, GroundLayer);
                    float newDist = transform.position.y - groundHit.point.y;
                    if (dist > newDist) dist = newDist;
                }
            }
            groundDistance = (float)System.Math.Round(dist, 2);
        }
    }

    private bool SnapToGround()
    {
        if (m_StepsSinceLastGrounded > 1 || m_StepsSinceLastJump <= 2)
        {
            return false;
        }
        if (!Physics.Raycast(Body.position, Vector3.down, out RaycastHit hit, GroundDistance, GroundLayer))
        {
            return false;
        }
        if (hit.normal.y < minGroundDotProduct)
        {
            return false;
        }
        m_OnSnapGround = true;

        var normal = hit.normal;
        CheckContact(normal);

        float speed = m_Velocity.magnitude;
        float dot = Vector3.Dot(m_Velocity, hit.normal);
        if (dot > 0f)
        {
            m_Velocity = (m_Velocity - hit.normal * dot).normalized * speed;
        }

        return true;
    }

    private void StepClimb()
    {
        RaycastHit hitLower;
        if (Physics.Raycast(Body.position + Vector3.up * 0.1f, transform.TransformDirection(Vector3.forward), out hitLower, m_Collider.radius + 0.1f))
        {
            RaycastHit hitUpper;
            if (hitLower.normal.y < 0.1 && !Physics.Raycast(Body.position + Vector3.up * StepHeight, transform.TransformDirection(Vector3.forward), out hitUpper, m_Collider.radius + 0.2f))
            {
                Body.position -= new Vector3(0f, -StepSmooth * Time.fixedDeltaTime, 0f);
            }
        }

        RaycastHit hitLower45;
        if (Physics.Raycast(Body.position + Vector3.up * 0.1f, transform.TransformDirection(1.5f, 0, 1), out hitLower45, m_Collider.radius + 0.1f))
        {
            RaycastHit hitUpper45;
            if (hitLower.normal.y < 0.1 && !Physics.Raycast(Body.position + Vector3.up * StepHeight, transform.TransformDirection(1.5f, 0, 1), out hitUpper45, m_Collider.radius + 0.2f))
            {
                Body.position -= new Vector3(0f, -StepSmooth * Time.fixedDeltaTime, 0f);
            }
        }

        RaycastHit hitLowerMinus45;
        if (Physics.Raycast(Body.position + Vector3.up * 0.1f, transform.TransformDirection(-1.5f, 0, 1), out hitLowerMinus45, m_Collider.radius + 0.1f))
        {
            RaycastHit hitUpperMinus45;
            if (hitLower.normal.y < 0.1 && !Physics.Raycast(Body.position + Vector3.up * StepHeight, transform.TransformDirection(-1.5f, 0, 1), out hitUpperMinus45, m_Collider.radius + 0.2f))
            {
                Body.position -= new Vector3(0f, -StepSmooth * Time.fixedDeltaTime, 0f);
            }
        }
    }

    #endregion 状态检测

    #region 调用接口

    public void DesiredMove(Vector3 inputMovement)
    {
        m_DesiredMove = true;
        m_StopMove = false;
        m_PreInput = m_InputMovement;
        m_InputMovement = inputMovement;
    }

    public void StopMove()
    {
        m_StopMove = true;
        m_PreInput = m_InputMovement = Vector3.zero;
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

    public void JumpInputStop()
    {
        m_JumpPerform = false;
    }

    public void ProcessAttack()
    {
        if (!m_JumpPerform)
        {
            //PatternAttacker?.TryAttack(Self.CurController);
        }
    }

    public void Sprint()
    {
        m_Sprint = m_OnGround || m_OnSnapGround ? SprintCoefficient : 1;
        m_OnWalk = false;
        m_Walk = 1;
    }

    public void SprintStop()
    {
        m_Sprint = 1;
    }

    public void WalkState()
    {
        m_OnWalk = !m_OnWalk;
        if (m_OnWalk)
        {
            m_Walk = WalkCoefficient;
        }
        else
        {
            m_Walk = 1;
        }
    }

    #endregion 调用接口
}