using UnityEngine;
using System.Collections;
using System;

public class ActorMove : MonoBehaviour
{
    // 角色移动相关
    public float moveSpeed = 50f;
    private Vector2 moveDirection = Vector2.zero;
    private Rigidbody rb;
    private Animator animator;
    private AutoNavigation autoNavigation;

    // 单例模式
    //private static ActorMove _instance;
    //public static ActorMove Instance
    //{
    //    get { return _instance; }
    //}

    //private void Awake()
    //{
    //    if (_instance != null && _instance != this)
    //    {
    //        Destroy(this.gameObject);
    //    }
    //    else
    //    {
    //        _instance = this;
    //        DontDestroyOnLoad(this.gameObject);
    //    }
    //}

    void Start()
    {
        animator = GetComponent<Animator>();
        // 获取角色的Rigidbody组件
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            // 如果没有Rigidbody，添加一个
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false; // 不需要重力
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // 记录自动导航组件，手动输入时用于切回手动模式
        autoNavigation = GetComponent<AutoNavigation>();
    }

    void Update()
    {
        //animator.SetFloat("x",Input.GetAxis("Horizontal"));
        //animator.SetFloat("y",Input.GetAxis("Vertical"));
        // 更新逻辑在Update中处理

    }

    void FixedUpdate()
    {
        // 使用FixedUpdate处理物理移动
        if (rb != null && moveDirection != Vector2.zero)
        {
            //Vector3 movement = new Vector3(moveDirection.x, 0, moveDirection.y) * moveSpeed * Time.fixedDeltaTime;
            //rb.MovePosition(transform.position + movement);
          
            animator.SetFloat("x", moveDirection.x);
            animator.SetFloat("y", -moveDirection.y);
        }
        else
        {
            animator.SetFloat("x", 0);
            animator.SetFloat("y", 0);
        }
    }

    // 处理从WebSocket接收到的移动命令
    public void ProcessMoveCommand(string message)
    {
        try
        {
            // 使用简单的字符串解析获取x和y值
            float x = 0, y = 0;

            // 简单解析示例（不依赖JsonUtility）
            int xIndex = message.IndexOf("\"x\":") + 4;
            int commaIndex = message.IndexOf(",", xIndex);
            if (xIndex > 0 && commaIndex > 0)
            {
                string xStr = message.Substring(xIndex, commaIndex - xIndex).Trim();
                float.TryParse(xStr, out x);
            }

            int yIndex = message.IndexOf("\"y\":") + 4;
            int endIndex = message.IndexOf("}", yIndex);
            if (yIndex > 0 && endIndex > 0)
            {
                string yStr = message.Substring(yIndex, endIndex - yIndex).Trim();
                float.TryParse(yStr, out y);
            }

            // 更新移动方向
            moveDirection = new Vector2(x, y);

            // 有摇杆输入且当前在自动导航，切换为手动模式
            if (autoNavigation != null &&
                autoNavigation.IsAutoNavigating &&
                moveDirection.sqrMagnitude > 0.0001f)
            {
                autoNavigation.StopAutoNavigationByManualControl();
            }

            //Debug.Log($"角色移动方向: ({x}, {y})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析移动命令失败: {ex.Message}");
        }
    }

    // 停止角色移动
    public void StopMovement()
    {
        moveDirection = Vector2.zero;
        Debug.Log("角色已停止移动");
    }

    // 设置移动速度
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        Debug.Log($"角色移动速度已设置为: {speed}");
    }
}