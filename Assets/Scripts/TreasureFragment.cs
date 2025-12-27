using UnityEngine;
using System.Collections;

[System.Serializable]
public class TreasureFoundMessage
{
    public string type = "treasure_found"; // 消息类型
    public string clientId; // 收集者ID
    public int fragmentIndex; // 碎片索引
    public int count; // 该玩家当前收集总数
}

public class TreasureFragment : MonoBehaviour
{
    public float collectDistance = 150f; // 收集距离
    public int fragmentIndex; // 碎片索引（0:1号，1:2号，2:3号，需在Inspector手动赋值）
    private bool isRespawning = false; // 是否正在重生冷却

    void Update()
    {
        if (isRespawning || UnitySever.Instance == null) return;

        // 遍历所有玩家，检测是否有玩家进入收集范围
        foreach (var client in UnitySever.Instance.connectedClients)
        {
            string clientId = client.Key;
            GameObject playerObject = client.Value;

            // 计算距离
            float distance = Vector3.Distance(transform.position, playerObject.transform.position);
            if (distance <= collectDistance)
            {
                // 尝试让该玩家收集碎片
                TryCollectByPlayer(clientId);
                break; // 一次只允许一个玩家收集
            }
        }
    }

    // 尝试让指定玩家收集碎片
    private void TryCollectByPlayer(string clientId)
    {
        var npc = FindObjectOfType<NPCInteraction>();
        if (npc == null) return;

        // 第一步：先判断玩家是否有任务状态（无状态则直接返回）
        if (!npc.playerTaskStates.TryGetValue(clientId, out var state))
        {
            Debug.Log($"玩家 {clientId} 无任务状态，无法收集碎片 {fragmentIndex}");
            return;
        }

        // 第二步：校验3个核心条件（新增「任务已启动」校验 ✅）
        // 1. 任务未启动 → 禁止收集
        // 2. 已收集过该碎片 → 禁止收集
        if (!state.isTaskStarted || state.collectedFragmentIds.Contains(fragmentIndex))
        {
            if (!state.isTaskStarted)
            {
                Debug.Log($"玩家 {clientId} 未接受任务，无法收集碎片 {fragmentIndex}");
            }
            else
            {
                Debug.Log($"玩家 {clientId} 已收集过碎片 {fragmentIndex}，无法重复收集");
            }
            return;
        }

        // 所有条件满足，才允许收集碎片
        CollectFragment(clientId);
    }


private void CollectFragment(string clientId)
{
    isRespawning = true;
    Debug.Log($"玩家 {clientId} 收集了碎片 {fragmentIndex}，3秒后重生");

    var npc = FindObjectOfType<NPCInteraction>();
    npc?.OnFragmentCollectedByPlayer(clientId, fragmentIndex);
    SendTreasureFoundMessage(clientId, npc, fragmentIndex);

    Debug.Log($"准备启动重生协程，碎片 {fragmentIndex}");
    
    // ✅ 改为通过服务器对象启动协程
    UnitySever.Instance.StartCoroutine(RespawnAfterDelay(5f));
    
    Debug.Log($"协程已启动，碎片 {fragmentIndex}");
    gameObject.SetActive(false);
}


private IEnumerator RespawnAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);

    if (this != null && gameObject != null)
    {
        gameObject.SetActive(true);
        isRespawning = false;
        Debug.Log($"碎片 {fragmentIndex} 已重生");
    }
}



    // 向所有客户端发送碎片收集消息（用于更新UI）
    private void SendTreasureFoundMessage(string clientId, NPCInteraction npc, int index)
    {
        if (npc.playerTaskStates.TryGetValue(clientId, out var state))
        {
            TreasureFoundMessage message = new TreasureFoundMessage
            {
                clientId = clientId,
                fragmentIndex = index,
                count= state.collectedFragmentIds.Count
            };
            string json = JsonUtility.ToJson(message);
            //UnitySever.Instance?.BroadcastMessage(json);
            UnitySever.Instance?.SendMessageToClient(clientId,json); 
        }
    }
}