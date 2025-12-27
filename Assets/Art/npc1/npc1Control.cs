using UnityEngine;
using UnityEngine.UI;

public class npc1Control : MonoBehaviour, INPCInteractable
{
    private Animator m_Anim;
    public bool HasTalked { get; set; } = false;
    [SerializeField]
    public Button dialogue;
    [SerializeField]
    public GameObject taskWarning;

    // 对话状态
    private bool isTalking = false;

    // 对话时面向的主角
    private Transform talkingTarget = null;

    // NPC 转向速度
    public float faceTurnSpeed = 3f;

    void Start()
    {
        m_Anim = GetComponent<Animator>();
        PlayGreet();
    }

    void Update()
    {
        if (!HasTalked)
        {
            taskWarning.SetActive(true);
        }
        else
        {
            taskWarning.SetActive(false);
        }
        // 如果在对话状态，持续面向主角
        if (isTalking && talkingTarget != null)
        {
            RotateTowardsTarget();
        }
    }

    void PlayGreet()
    {
        m_Anim.SetBool("greet", true);
        m_Anim.SetBool("talking", false);
        isTalking = false;
        talkingTarget = null;
    }

    public void EnterTalking(Transform target)
    {
        if (HasTalked) return;
        HasTalked = true;
        m_Anim.SetBool("greet", false);
        m_Anim.SetBool("talking", true);
        isTalking = true;

        talkingTarget = target; // 保存主角 Transform
    }

    public void ExitTalking()
    {
        PlayGreet();
    }

    private void RotateTowardsTarget()
    {
        if (talkingTarget == null) return;

        Vector3 targetDir = talkingTarget.position - transform.position;
        targetDir.y = 0f; // 忽略高度差
        if (targetDir == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(targetDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * faceTurnSpeed);
    }

    // 实现 INPCInteractable 接口方法
    public void Interact()
    {
        // 这里实现交互逻辑
        // 例如：打开对话界面、触发任务等
        Debug.Log("开始与NPC对话");
        
        // 如果你有对话系统，可以在这里调用
        // DialogueManager.StartDialogue(dialogueData);
    }

    // 实现 INPCInteractable 接口方法
    public string GetInteractionText()
    {
        // 返回交互时显示的提示文本
        if (HasTalked)
        {
            return "再次对话";
        }
        else
        {
            return "按E与NPC对话";
        }
    }
}