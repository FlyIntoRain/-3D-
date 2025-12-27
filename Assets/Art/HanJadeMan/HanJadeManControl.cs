//using System.Collections;
//using UnityEngine;
//using System.Linq;

//public class HanJadeMancontrol : MonoBehaviour
//{
//    private Animator m_Anim;

//    // 当前动画状态
//    private bool isWalking = false;
//    private bool isRunning = false;
//    private bool isBackwards = false;
//    private bool isLeftTurning = false;
//    private bool isRightTurning = false;
//    private bool isSitting = false;
//    private bool isStandingUp = false;
//    private bool isTalking = false;

//    private float baseY;

//    // NPC 引用
//    private MonoBehaviour currentNPC = null;
//    private INPCInteractable[] allNPCs;

//    // 自动转向速度（越大转得越快）
//    public float faceTurnSpeed = 3f;

//    void Start()
//    {
//        m_Anim = GetComponent<Animator>();
//        baseY = transform.position.y;
//        allNPCs = FindObjectsOfType<MonoBehaviour>().OfType<INPCInteractable>().ToArray();
//    }

//    void Update()
//    {
//        // 若在与 NPC 对话状态中，则禁止其他控制，并持续朝向 NPC
//        if (isTalking)
//        {
//            if (currentNPC != null)
//                RotateTowardsNPC();

//            // 👉 任意方向键 / Shift / Ctrl 离开对话
//            if (Input.anyKeyDown)
//            {
//                ExitTalking();
//                return;
//            }

//            return;
//        }

//        bool anyAction = false;

//        HandleUpArrowInput(ref anyAction);
//        HandleDownArrowInput(ref anyAction);

//        if (!anyAction && Input.GetKey(KeyCode.LeftArrow))
//        {
//            LeftTurn();
//            anyAction = true;
//        }

//        if (!anyAction && Input.GetKey(KeyCode.RightArrow))
//        {
//            RightTurn();
//            anyAction = true;
//        }

//        if (!anyAction && !isSitting && !isStandingUp)
//        {
//            SetIdleState();
//        }
//    }

//    void LateUpdate()
//    {
//        // 锁定Y高度
//        Vector3 pos = transform.position;
//        pos.y = baseY;
//        transform.position = pos;
//    }

//    // ================= 上箭头逻辑 ==================
//    private void HandleUpArrowInput(ref bool anyAction)
//    {
//        if (Input.GetKey(KeyCode.UpArrow))
//        {
//            if (isSitting)
//            {
//                StandUp();
//                anyAction = true;
//                return;
//            }

//            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
//                Run();
//            else
//                Walk();

//            anyAction = true;
//        }

//        if (Input.GetKeyUp(KeyCode.UpArrow) && !isStandingUp && !isSitting)
//        {
//            SetIdleState();
//        }
//    }

//    // ================= 下箭头逻辑 ==================
//    private void HandleDownArrowInput(ref bool anyAction)
//    {
//        if (Input.GetKey(KeyCode.DownArrow))
//        {
//            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
//                Sit();
//            else if (!isSitting)
//                Backwards();

//            anyAction = true;
//        }

//        if (Input.GetKeyUp(KeyCode.DownArrow))
//        {
//            if (!isSitting)
//                SetIdleState();
//        }
//    }

//    // ================= 动画控制函数 =================
//    void Walk()
//    {
//        if (!isWalking)
//        {
//            ResetAllAnimBools();
//            m_Anim.SetBool("walk", true);
//            isWalking = true;
//        }
//    }

//    void Run()
//    {
//        if (!isRunning)
//        {
//            ResetAllAnimBools();
//            m_Anim.SetBool("run", true);
//            isRunning = true;
//        }
//    }

//    void Backwards()
//    {
//        if (!isBackwards)
//        {
//            ResetAllAnimBools();
//            m_Anim.SetBool("backwards", true);
//            isBackwards = true;
//        }
//    }

//    void LeftTurn()
//    {
//        if (!isLeftTurning)
//        {
//            ResetAllAnimBools();
//            m_Anim.SetBool("left_turn", true);
//            isLeftTurning = true;
//        }
//    }

//    void RightTurn()
//    {
//        if (!isRightTurning)
//        {
//            ResetAllAnimBools();
//            m_Anim.SetBool("right_turn", true);
//            isRightTurning = true;
//        }
//    }

//    void Sit()
//    {
//        if (!isSitting)
//        {
//            ResetAllAnimBools();
//            m_Anim.SetBool("sit", true);
//            isSitting = true;
//        }
//    }

//    void StandUp()
//    {
//        if (isSitting && !isStandingUp)
//        {
//            ResetAllAnimBools();
//            m_Anim.SetBool("standup", true);
//            isStandingUp = true;
//            isSitting = false;
//            StartCoroutine(WaitForStandUpToFinish());
//        }
//    }

//    IEnumerator WaitForStandUpToFinish()
//    {
//        yield return new WaitForSeconds(1.2f);
//        isStandingUp = false;
//        SetIdleState();
//    }

//    void SetIdleState()
//    {
//        if (isWalking || isRunning || isBackwards || isLeftTurning || isRightTurning || isStandingUp)
//        {
//            ResetAllAnimBools();
//            m_Anim.SetBool("idle", true);

//            isWalking = false;
//            isRunning = false;
//            isBackwards = false;
//            isLeftTurning = false;
//            isRightTurning = false;
//            isStandingUp = false;
//        }
//    }

//    private void ResetAllAnimBools()
//    {
//        m_Anim.SetBool("walk", false);
//        m_Anim.SetBool("run", false);
//        m_Anim.SetBool("backwards", false);
//        m_Anim.SetBool("left_turn", false);
//        m_Anim.SetBool("right_turn", false);
//        m_Anim.SetBool("sit", false);
//        m_Anim.SetBool("idle", false);
//        m_Anim.SetBool("standup", false);
//        m_Anim.SetBool("talking", false);
//    }

//    // 与NPC交互的最大距离（比如1.5米）
//    public float talkRange = 1.5f;

//    void FixedUpdate()
//    {
//        HandleNPCInteraction();
//    }

//    private void HandleNPCInteraction()
//    {
//        foreach (var npc in allNPCs)
//        {
//            if (npc.HasTalked) continue;
//            float dist = Vector3.Distance(transform.position, ((MonoBehaviour)npc).transform.position);
//            if (dist <= talkRange && !isTalking)
//            {
//                currentNPC = (MonoBehaviour)npc;
//                EnterTalking();
//                break;
//            }
//        }
//    }


//    private void EnterTalking()
//    {
//        ResetAllAnimBools();
//        m_Anim.SetBool("talking", true);
//        isTalking = true;

//        if (currentNPC != null)
//        {
//            INPCInteractable npc = currentNPC as INPCInteractable;
//            npc?.EnterTalking(transform);
//        }

//        RotateTowardsNPC(true);
//    }

//    private void ExitTalking()
//    {
//        if (isTalking)
//        {
//            ResetAllAnimBools();
//            m_Anim.SetBool("idle", true);
//            isTalking = false;
//            if (currentNPC != null)
//            {
//                INPCInteractable npc = currentNPC as INPCInteractable;
//                npc?.ExitTalking();
//            }
//        }
//    }

//    // ================= 面向 NPC 功能 =================
//    private void RotateTowardsNPC(bool instant = false)
//    {
//        if (currentNPC == null) return;

//        Vector3 targetDir = currentNPC.transform.position - transform.position;
//        targetDir.y = 0f; // 忽略高度差
//        if (targetDir == Vector3.zero) return;

//        Quaternion targetRot = Quaternion.LookRotation(targetDir);

//        if (instant)
//            transform.rotation = targetRot;
//        else
//            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * faceTurnSpeed);
//    }
//}
