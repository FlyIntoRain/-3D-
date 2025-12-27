using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class AutoNavigation : MonoBehaviour
{
    [Header("导航目标")]
    public GameObject target;

    [Header("动画设置")]
    public Animator animator;

    [Header("导航设置")]
    [Tooltip("停止距离")]
    public float stoppingDistance = 15.0f;

    [Header("速度设置")]
    [Tooltip("导航时的移动速度（建议填跑步速度）")]
    public float navMoveSpeed = 6.0f;
    [Tooltip("导航时的加速度，数值越大越快进入全速")]
    public float navAcceleration = 20.0f;
    [Tooltip("导航时的转向速度")]
    public float navAngularSpeed = 720.0f;
    [Tooltip("用于归一化动画的最大移动速度（通常与navMoveSpeed一致）")]
    public float navAnimMaxSpeed = 6.0f;

    [Header("动画强度设置")]
    [Tooltip("X轴动画强度")]
    public float xIntensity = 1.0f;
    [Tooltip("Y轴动画强度")]
    public float yIntensity = 1.0f;

    [Header("其他设置")]
    public bool autoStartOnPlay = true;
    public bool showDebugInfo = false;

    [Header("调试选项")]
    [Tooltip("是否允许启动自动寻路（方便在运行时手动开关调试）")]
    public bool enableAutoNavigation = true;
    
    [Tooltip("在运行时勾选即可立刻开始一次自动寻路（仅调试用）")]
    public bool debugStartNavigation = false;

    // 内部记录上一次的勾选状态，用来检测 Inspector 中是否从 false -> true
    private bool _lastDebugStartNavigation = false;

    private NavMeshAgent agent;                 // 仅在自动导航时动态添加
    private Rigidbody rb;
    private RigidbodyConstraints originalConstraints;
    private bool originalIsKinematic;
    private bool hasRigidbody = false;
    private Vector3 lastPosition;
    private float currentX, currentY;

    private bool isAutoNavigating = false;      // 是否处于自动寻路中

    // 对外公开当前是否在自动导航
    public bool IsAutoNavigating => isAutoNavigating;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            hasRigidbody = true;
            originalConstraints = rb.constraints;
            originalIsKinematic = rb.isKinematic;
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        lastPosition = transform.position;
        currentX = 0;
        currentY = 0;
        isAutoNavigating = false;
    }

    void Update()
    {
        // 1. 检测 Inspector 中的“调试启动”勾选是否从 false -> true，如果是就启动一次自动寻路
        if (debugStartNavigation && !_lastDebugStartNavigation)
        {
            // 立刻重置为 false，避免每帧重复触发
            debugStartNavigation = false;
            _lastDebugStartNavigation = false;

            // 调用统一的外部接口，自动检查 enableAutoNavigation、target 等
            StartAutoNavigationExternal();
        }
        else
        {
            _lastDebugStartNavigation = debugStartNavigation;
        }

        // 2. 正常的自动寻路更新逻辑
        // 只有在“正在自动寻路”且存在有效 NavMeshAgent 时才更新导航与动画
        if (isAutoNavigating && target != null && agent != null && agent.enabled)
        {
            if (agent.hasPath)
            {
                float targetDistance = Vector3.Distance(agent.destination, target.transform.position);
                if (targetDistance > 1.0f)
                {
                    agent.SetDestination(target.transform.position);
                }
            }
            else if (agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathInvalid)
            {
                agent.SetDestination(target.transform.position);
            }

            UpdateAnimation();

            if (agent.hasPath &&
                agent.remainingDistance <= agent.stoppingDistance &&
                agent.velocity.sqrMagnitude < 0.1f)
            {
                StopAutoNavigation();
                Debug.Log("已到达目的地");
            }
        }
    }

    void UpdateAnimation()
    {
        if (animator == null || agent == null || !agent.enabled) return;

        Vector3 velocity = agent.velocity;
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);

        // 计算归一化速度，让 Blend Tree 能到达“跑”区间
        float maxSpeed = Mathf.Max(0.1f, navAnimMaxSpeed > 0 ? navAnimMaxSpeed : agent.speed);
        float forwardNorm = Mathf.Clamp(localVelocity.z / maxSpeed, -1f, 1f);

        float targetX = Mathf.Clamp(localVelocity.x * xIntensity, -1f, 1f);
        // Blend Tree 如果前进阈值为正（0.7/1.0），用正向数值推到跑步区
        float targetY = Mathf.Clamp(forwardNorm * yIntensity, -1f, 1f);

        currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * 10);
        currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * 10);

        animator.SetFloat("x", currentX);
        animator.SetFloat("y", currentY);

        if (showDebugInfo && velocity.magnitude > 0.1f)
        {
            Debug.Log($"动画参数: x={currentX:F2}, y={currentY:F2}, 速度={velocity.magnitude:F2}");
        }
    }

    // 动态创建 NavMeshAgent（仅在需要自动导航时调用）
    private void SetupNavMeshAgent()
    {
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            ConfigureNavMeshAgent();
        }
        else if (!agent.enabled)
        {
            agent.enabled = true;
            ConfigureNavMeshAgent();
        }
    }

    private void ConfigureNavMeshAgent()
    {
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
        agent.autoRepath = true;
        
        // 速度参数：确保 NavMeshAgent 本身使用“跑步”速度
        agent.speed = navMoveSpeed;
        agent.acceleration = navAcceleration;
        agent.angularSpeed = navAngularSpeed;
        
        // 设置合适的代理参数
        agent.radius = 0.5f;
        agent.height = 2.0f;
        agent.baseOffset = 0.0f;
    }

    public void StartAutoNavigation()
    {
        // 调试总开关：关闭时，任何启动自动寻路的调用都会被忽略
        if (!enableAutoNavigation)
        {
            Debug.LogWarning("AutoNavigation: 当前已禁用自动寻路（enableAutoNavigation = false），不会启动导航。");
            return;
        }

        if (target == null)
        {
            Debug.LogError("AutoNavigation: 目标未设置");
            return;
        }

        // 动态创建或启用 NavMeshAgent（此时才真正添加 / 启用 agent）
        SetupNavMeshAgent();

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("AutoNavigation: 角色不在NavMesh上");
            return;
        }

        // 处理刚体，确保可以移动但不会干扰旋转
        if (hasRigidbody && rb != null)
        {
            // 保存当前状态
            originalConstraints = rb.constraints;
            originalIsKinematic = false ;
            
            // 导航期间让刚体不参与物理推挤，避免漂浮感
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // 只锁定X和Z轴旋转，允许Y轴旋转
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        bool pathSet = agent.SetDestination(target.transform.position);
        if (!pathSet)
        {
            Debug.LogWarning($"AutoNavigation: 无法设置到 {target.name} 的路径，目标可能不在 NavMesh 上");
        }

        isAutoNavigating = true;
        Debug.Log($"开始自动导航到: {target.name}");

        agent.isStopped = false;
        currentX = 0;
        currentY = 0;
    }

    public void StopAutoNavigation()
    {
        if (agent != null)
        {
            agent.ResetPath();
            agent.isStopped = true;
            isAutoNavigating = false;

            // 完全恢复刚体状态，让其它脚本可以正常控制旋转 / 移动
            if (hasRigidbody && rb != null)
            {
                rb.isKinematic = originalIsKinematic;
                rb.constraints = originalConstraints;
            }

            if (animator != null)
            {
                animator.SetFloat("x", 0);
                animator.SetFloat("y", 0);
            }

            currentX = 0;
            currentY = 0;

            Debug.Log("已停止自动导航");

            // 导航结束后，销毁 NavMeshAgent，确保在非自动导航时完全不会影响遥感控制
            DestroyNavMeshAgent();
        }
    }

    // 手动操控时调用：若当前处于自动导航则立即停止
    public void StopAutoNavigationByManualControl()
    {
        if (!isAutoNavigating) return;

        Debug.Log("AutoNavigation: 检测到手动输入，停止自动导航，切换为手动模式。");
        StopAutoNavigation();
    }

    public void SetTargetAndNavigate(GameObject newTarget)
    {
        target = newTarget;
        StartAutoNavigation();
    }

    public void TriggerAutoNavigation(GameObject navigationTarget = null)
    {
        if (navigationTarget != null)
        {
            target = navigationTarget;
        }

        StartAutoNavigation();
    }

    /// <summary>
    /// 提供给其他脚本调用的简化接口：
    /// - 可选传入一个新的目标
    /// - 会自动检查 enableAutoNavigation 开关
    /// 使用示例：autoNav.StartAutoNavigationExternal(targetObj);
    /// </summary>
    public void StartAutoNavigationExternal(GameObject navigationTarget = null)
    {
        if (navigationTarget != null)
        {
            target = navigationTarget;
        }

        StartAutoNavigation();
    }
    
    public void AdjustStoppingDistance(float newDistance)
    {
        stoppingDistance = newDistance;
        if (agent != null)
        {
            agent.stoppingDistance = stoppingDistance;
        }
        Debug.Log($"停止距离已调整为: {stoppingDistance}");
    }

    public void SetAnimationIntensity(float xIntensity, float yIntensity)
    {
        this.xIntensity = xIntensity;
        this.yIntensity = yIntensity;
        Debug.Log($"动画强度设置: X={xIntensity}, Y={yIntensity}");
    }
    
    // 完全销毁 NavMeshAgent：在停止自动导航时调用
    public void DestroyNavMeshAgent()
    {
        if (agent != null)
        {
            Destroy(agent);
            agent = null;
        }
    }
}