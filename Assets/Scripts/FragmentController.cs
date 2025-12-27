using UnityEngine;

public class FragmentController : MonoBehaviour
{
    [Header("碎片设置")]
    public GameObject[] fragmentPrefabs; // 碎片预制体数组，索引0对应fragment1

    [Header("显示设置")]
    public Vector3 offset = new Vector3(0, 2f, 0);

    [Header("旋转设置")]
    public float rotationSpeed = 10f;

    private GameObject currentFragment;
    private bool isFragmentVisible = false;

    // 旋转控制
    private float targetYRotation = 0f;
    private float currentYRotation = 0f;

    void Update()
    {
        // 如果碎片可见，更新位置
        if (isFragmentVisible && currentFragment != null)
        {
            UpdateFragmentPosition();

            // 平滑旋转到目标角度
            if (Mathf.Abs(currentYRotation - targetYRotation) > 0.01f)
            {
                currentYRotation = Mathf.Lerp(currentYRotation, targetYRotation, rotationSpeed * Time.deltaTime);

                // ✅ 保持X和Z轴不变，只旋转Y轴
                Vector3 currentEuler = currentFragment.transform.eulerAngles;
                currentFragment.transform.rotation = Quaternion.Euler(
                    currentEuler.x,      // 保持X轴不变
                    currentYRotation,    // 旋转Y轴
                    currentEuler.z       // 保持Z轴不变
                );
            }
        }
    }

    // 显示指定碎片
    public void ShowFragment(string fragmentId)
    {
        // 从fragment1, fragment2...转换为索引
        int fragmentIndex = GetFragmentIndex(fragmentId);

        if (fragmentIndex < 0 || fragmentIndex >= fragmentPrefabs.Length)
        {
            Debug.LogError($"碎片索引无效: {fragmentId} -> 索引{fragmentIndex}");
            return;
        }

        if (fragmentPrefabs[fragmentIndex] == null)
        {
            Debug.LogError($"碎片预制体未设置: 索引{fragmentIndex}");
            return;
        }

        // 如果已有显示的碎片，先销毁
        if (currentFragment != null)
        {
            Destroy(currentFragment);
        }

        // 创建碎片实例
        currentFragment = Instantiate(fragmentPrefabs[fragmentIndex]);
        currentFragment.name = $"PlayerFragment_{fragmentId}";

        // ✅ 核心修改：只对第二块和第三块碎片设置X旋转为-90
        // fragmentIndex 0 = fragment1, 1 = fragment2, 2 = fragment3
        if (fragmentIndex == 1 || fragmentIndex == 2) // fragment2 或 fragment3
        {
            // 获取当前的世界旋转
            Vector3 currentRotation = currentFragment.transform.eulerAngles;

            // 设置X为-90度（Unity内部存储为270度）
            currentFragment.transform.rotation = Quaternion.Euler(-90f, currentRotation.y, currentRotation.z);

            Debug.Log($"特殊处理碎片 {fragmentId}: 设置X旋转为-90度");
        }

        // 初始化旋转控制变量
        currentYRotation = currentFragment.transform.eulerAngles.y;
        targetYRotation = currentYRotation;

        currentFragment.SetActive(true);
        isFragmentVisible = true;

        UpdateFragmentPosition();

        Debug.Log($"显示碎片: {fragmentId} (索引{fragmentIndex}), 最终旋转: {currentFragment.transform.eulerAngles}");
    }

    // 隐藏碎片（原来的方法，只是隐藏）
    public void HideFragment()
    {
        if (currentFragment != null)
        {
            currentFragment.SetActive(false);
            isFragmentVisible = false;
            Debug.Log("隐藏碎片");
        }
    }

    // ✅ 新增：强制销毁碎片（当手机端关闭展示时调用）
    public void ForceDestroyFragment()
    {
        if (currentFragment != null)
        {
            Debug.Log($"强制销毁碎片: {currentFragment.name}");
            Destroy(currentFragment);
            currentFragment = null;
            isFragmentVisible = false;
        }
        else
        {
            Debug.Log("没有碎片可销毁");
        }
    }

    // 设置Y轴旋转
    public void SetRotation(float yRotation)
    {
        // 设置目标旋转角度（网页端传来的是弧度，转换为角度）
        targetYRotation = yRotation * Mathf.Rad2Deg;

        // ✅ 添加详细调试信息
        Debug.Log($"设置碎片旋转到: {targetYRotation} 度 (原始弧度: {yRotation})");
        Debug.Log($"当前旋转: {currentYRotation} 度, 目标旋转: {targetYRotation} 度");

        // ✅ 立即应用一次旋转，避免延迟
        currentYRotation = targetYRotation;
        if (currentFragment != null)
        {
            // ✅ 正确方法：只修改Y轴，保持X和Z轴不变
            Vector3 currentEuler = currentFragment.transform.eulerAngles;
            currentFragment.transform.rotation = Quaternion.Euler(
                currentEuler.x,      // 保持X轴不变（应该是270/-90）
                currentYRotation,    // 旋转Y轴
                currentEuler.z       // 保持Z轴不变
            );

            Debug.Log($"✅ 应用旋转: X={currentFragment.transform.eulerAngles.x}, Y={currentFragment.transform.eulerAngles.y}");
        }
    }

    // 更新碎片位置
    private void UpdateFragmentPosition()
    {
        if (currentFragment != null)
        {
            currentFragment.transform.position = transform.position + offset;
        }
    }

    // 检查碎片是否可见
    public bool IsFragmentVisible()
    {
        return isFragmentVisible;
    }

    // 从fragment1, fragment2...转换为索引0, 1...
    private int GetFragmentIndex(string fragmentId)
    {
        if (string.IsNullOrEmpty(fragmentId) || !fragmentId.StartsWith("fragment"))
        {
            return -1;
        }

        string numberStr = fragmentId.Replace("fragment", "");
        if (int.TryParse(numberStr, out int number))
        {
            return number - 1; // fragment1对应索引0
        }

        return -1;
    }

    void DebugFragmentTransform()
    {
        if (currentFragment != null)
        {
            Debug.Log($"碎片Transform信息:");
            Debug.Log($"- 局部Rotation: {currentFragment.transform.localEulerAngles}");
            Debug.Log($"- 世界Rotation: {currentFragment.transform.eulerAngles}");
            Debug.Log($"- 局部Scale: {currentFragment.transform.localScale}");
            Debug.Log($"- 世界Position: {currentFragment.transform.position}");

            // 检查父对象的transform
            if (currentFragment.transform.parent != null)
            {
                Debug.Log($"父对象Rotation: {currentFragment.transform.parent.eulerAngles}");
            }
        }
    }

    void OnDestroy()
    {
        if (currentFragment != null)
        {
            Destroy(currentFragment);
        }
    }
}