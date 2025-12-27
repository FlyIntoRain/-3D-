using UnityEngine;

public class SimpleCamController : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("前后移动的速度")]
    public float moveSpeed = 10f;

    [Tooltip("左右旋转的速度")]
    public float turnSpeed = 60f; // 旋转速度通常要比移动数值大一些才灵敏

    void Update()
    {
        MoveAndRotate();
    }

    void MoveAndRotate()
    {
        // 1. 获取键盘输入
        // Input.GetAxis("Vertical") 对应 W/S 或 上/下箭头，返回 -1 到 1
        // Input.GetAxis("Horizontal") 对应 A/D 或 左/右箭头，返回 -1 到 1
        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        // 2. 计算移动量 (前后)
        // Vector3.forward 表示物体正前方
        // Time.deltaTime 保证移动速度在不同帧率下保持一致
        Vector3 moveDirection = Vector3.forward * moveInput * moveSpeed * Time.deltaTime;

        // 3. 计算旋转量 (左右)
        // Vector3.up 表示绕着Y轴（垂直轴）旋转
        float turnAmount = turnInput * turnSpeed * Time.deltaTime;

        // 4. 应用移动
        // 使用 Space.Self 确保是沿着“自己的”前方移动，而不是世界坐标的北方
        transform.Translate(moveDirection, Space.Self);

        // 5. 应用旋转
        transform.Rotate(Vector3.up, turnAmount);
    }
}