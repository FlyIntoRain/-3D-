using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class Baixing1 : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 targetPosition;
    private bool isWaitingForNewTarget = false; // 新增：防止重复Invoke

    public float minWaitTime = 1f;   // 到达目标点后等待时间
    public float maxWaitTime = 3f;
    public float minMoveDistance = 5f; // 最小寻路距离（避免短距离绕圈）
    public float maxMoveRange = 50f; // 最大寻路范围

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // 优化NavMeshAgent参数
        agent.autoBraking = false; // 避免急刹
        agent.stoppingDistance = 1f; // 停止距离设为1米
        
        FindNewTarget();
    }

    void Update()
    {
        // 更新动画状态
        bool isMoving = !HasReachedDestination();
        animator.SetBool("IsMoving", isMoving);

        // 如果到达目标点且未在等待，等待后重新选择目标
        if (HasReachedDestination() && !isWaitingForNewTarget)
        {
            isWaitingForNewTarget = true;
            Invoke("FindNewTarget", Random.Range(minWaitTime, maxWaitTime));
        }
    }

    void FindNewTarget()
    {
        Vector3 newTarget = transform.position;
        int maxAttempts = 10; // 防止无限循环
        int attempts = 0;

        // 循环生成符合最小距离的有效目标点
        while (Vector3.Distance(newTarget, transform.position) < minMoveDistance && attempts < maxAttempts)
        {
            NavMeshHit hit;
            Vector3 randomDirection = Random.insideUnitSphere * maxMoveRange;
            randomDirection += transform.position;
            
            if (NavMesh.SamplePosition(randomDirection, out hit, maxMoveRange, NavMesh.AllAreas))
            {
                newTarget = hit.position;
            }
            attempts++;
        }

        targetPosition = newTarget;
        agent.SetDestination(targetPosition);
        isWaitingForNewTarget = false; // 重置等待标记
    }

    bool HasReachedDestination()
    {
        // 优化到达判定：允许微小速度（避免因浮点误差误判）
        return !agent.pathPending && 
               agent.remainingDistance <= agent.stoppingDistance &&
               Mathf.Approximately(agent.velocity.sqrMagnitude, 0f);
    }
}