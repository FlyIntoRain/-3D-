using UnityEngine;
using System.Collections;

public class ArtifactController : MonoBehaviour
{
    [Header("文物设置")]
    public GameObject artifactPrefab;
    public Vector3 offset = new Vector3(0, 2f, 0);
    
    private GameObject currentArtifact;
    private bool isArtifactVisible = false;

    // 新增：旋转控制
    private float targetYRotation = 0f;
    private float currentYRotation = 0f;
    private float rotationSpeed = 10f;

    void Start()
    {
        // 初始化
    }

    void Update()
    {
        // 如果文物可见，更新位置
        if (isArtifactVisible && currentArtifact != null)
        {
            UpdateArtifactPosition();

            // 平滑旋转到目标角度
            if (Mathf.Abs(currentYRotation - targetYRotation) > 0.01f)
            {
                currentYRotation = Mathf.Lerp(currentYRotation, targetYRotation, rotationSpeed * Time.deltaTime);
                currentArtifact.transform.rotation = Quaternion.Euler(0, currentYRotation, 0);
            }
        }
    }

    // 显示文物
    public void ShowArtifact()
    {
        if (artifactPrefab == null)
        {
            Debug.LogError("文物预制体未设置！");
            return;
        }

        if (currentArtifact == null)
        {
            currentArtifact = Instantiate(artifactPrefab);
            currentArtifact.name = "PlayerArtifact";

            // 初始化旋转为0
            currentArtifact.transform.rotation = Quaternion.identity;
            currentYRotation = 0f;
            targetYRotation = 0f;
        }

        currentArtifact.SetActive(true);
        isArtifactVisible = true;

        UpdateArtifactPosition();

        Debug.Log($"在玩家 {gameObject.name} 上方显示文物");
    }

    // 隐藏文物
    public void HideArtifact()
    {
        if (currentArtifact != null)
        {
            currentArtifact.SetActive(false);
            isArtifactVisible = false;
            Debug.Log($"隐藏玩家 {gameObject.name} 的文物");
        }
    }

    // 设置Y轴旋转（水平旋转）
    public void SetRotation(float yRotation)
    {
        // 将弧度转换为角度（如果需要）
        // 或者直接使用接收到的角度值

        // 设置目标旋转角度
        targetYRotation = yRotation * Mathf.Rad2Deg; // 如果是弧度转换为角度
                                                     // 或者直接使用：targetYRotation = yRotation; // 如果已经是角度

        Debug.Log($"设置文物旋转到: {targetYRotation} 度");
    }

    // 更新文物位置
    private void UpdateArtifactPosition()
    {
        if (currentArtifact != null)
        {
            // 设置位置
            currentArtifact.transform.position = transform.position + offset;

            // 保持当前旋转
            // 旋转在Update中平滑处理
        }
    }

    // 检查文物是否可见
    public bool IsArtifactVisible()
    {
        return isArtifactVisible;
    }

    // 清理资源
    void OnDestroy()
    {
        if (currentArtifact != null)
        {
            Destroy(currentArtifact);
        }
    }
}