using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class InteractionMessage
{
    public string type;
    public string title;
    public string content;
}

[System.Serializable]
public class TaskStateMessage
{
    public string type;
    public string state;
}

public class NPCInteraction : MonoBehaviour
{
    public GameObject[] treasureFragments; // 全局碎片数组（0:碎片1，1:碎片2，2:碎片3）
    public float interactionDistance = 300f;
    // 存储每个玩家的任务状态：key=客户端ID，value=该玩家的状态
    internal Dictionary<string, PlayerTaskState> playerTaskStates = new Dictionary<string, PlayerTaskState>();

    // 单个玩家的任务状态
    internal class PlayerTaskState
    {
        public bool isTaskStarted = false; // 是否接受任务
        public bool isTaskCompleted = false; // 是否完成任务
        public bool isWaitingForTaskConfirm = false; // 是否在等待任务确认
        public HashSet<int> collectedFragmentIds = new HashSet<int>(); // 已收集的碎片ID（0,1,2）
        public bool isPuzzleCompleted = false;
    }

    void Update()
    {
        // 遍历所有在线玩家，更新每个玩家的交互状态
        if (UnitySever.Instance == null) return;
        foreach (var client in UnitySever.Instance.connectedClients)
        {
            string clientId = client.Key;
            GameObject playerObject = client.Value;

            // 计算玩家与NPC的距离
            float distance = Vector3.Distance(transform.position, playerObject.transform.position);
            bool inRange = distance <= interactionDistance;

            // 初始化玩家状态（若不存在）
            if (!playerTaskStates.TryGetValue(clientId, out var state))
            {
                state = new PlayerTaskState();
                playerTaskStates[clientId] = state;
            }

            // 判断玩家是否可交互（未开始任务 或 已开始且收集完所有碎片）
            bool canInteract = inRange && ((!state.isTaskStarted) ||
                                           (state.isTaskStarted && state.collectedFragmentIds.Count == treasureFragments.Length));

            // 向该玩家发送交互状态（单播）
            SendNPCStatusToPlayer(clientId, inRange, canInteract);
        }
    }

    // 向单个玩家发送NPC状态
    private void SendNPCStatusToPlayer(string clientId, bool inRange, bool canInteract)
    {
        var message = new
        {
            type = "npc_status",
            inRange = inRange,
            canInteract = canInteract
        };
        UnitySever.Instance?.SendMessageToClient(clientId, JsonUtility.ToJson(message));
    }

    public void HandleConfirmation(string clientId)
    {
        if (!playerTaskStates.TryGetValue(clientId, out var state)) return;
        if (!UnitySever.Instance.connectedClients.TryGetValue(clientId, out var playerObject)) return;

        // ✅ 距离检查：必须在交互范围内才能触发
        float distance = Vector3.Distance(transform.position, playerObject.transform.position);
        if (distance > interactionDistance)
        {
            Debug.Log($"玩家 {clientId} 距离NPC太远，无法交互 ({distance:F2}/{interactionDistance})");
            return;
        }

        // 以下是原有逻辑
        if (!state.isTaskStarted)
        {
            Debug.Log("123");
            SendTaskConfirmDialogToPlayer(clientId);
            state.isWaitingForTaskConfirm = true;
        }
        else if (!state.isPuzzleCompleted && state.collectedFragmentIds.Count == treasureFragments.Length)
        {
            CompleteTaskForPlayer(clientId);
        }
    }


    // 向单个玩家发送任务确认对话框
    private void SendTaskConfirmDialogToPlayer(string clientId)
    {
        InteractionMessage message = new InteractionMessage
        {
            type = "interaction",
            title = "接受任务？",
            content = "是否接受寻找9个文物碎片的任务？每个碎片只能收集一次，5秒后会重生供其他玩家收集。"
        };
        if (UnitySever.Instance != null)
        {
            Debug.Log("456");
            UnitySever.Instance.SendMessageToClient(clientId, JsonUtility.ToJson(message));
        }
    }

    // 处理玩家的对话框结果（接受/拒绝任务）
    public void HandleDialogResult(string clientId, bool isConfirm)
    {
        if (!playerTaskStates.TryGetValue(clientId, out var state) || !state.isWaitingForTaskConfirm) return;

        if (isConfirm)
        {
            // 接受任务：标记任务开始，显示所有碎片
            state.isTaskStarted = true;
            ShowAllFragments(); // 全局显示所有碎片
            SendMessageToClient(clientId, "任务开始", "请收集所有9个文物碎片（每个只能收集一次）");

            // 通知对应客户端：当前目标切换为“找寻三个碎片”
            TaskStateMessage taskStateMessage = new TaskStateMessage();
            taskStateMessage.type = "task_state";
            taskStateMessage.state = "collect_fragments";
            UnitySever.Instance?.SendMessageToClient(clientId, JsonUtility.ToJson(taskStateMessage));
        }
        else
        {
            // 拒绝任务
            SendMessageToClient(clientId, "任务取消", "你已拒绝任务，可随时再来找我。");
        }
        state.isWaitingForTaskConfirm = false;
    }

    // 单个玩家完成任务
    private void CompleteTaskForPlayer(string clientId)
    {
        if (!playerTaskStates.TryGetValue(clientId, out var state)) return;

        //state.isTaskCompleted = true;
        //此处触发拼图游戏
        SendMessageToClient(clientId, "任务结束", "恭喜你收集了所有碎片，完成任务！");
    }

    // 全局显示所有碎片（初始或重生时）
    public void ShowAllFragments()
    {
        foreach (var fragment in treasureFragments)
        {
            if (fragment != null) fragment.SetActive(true);
        }
    }

    // 记录玩家收集的碎片（由碎片脚本调用）
    public void OnFragmentCollectedByPlayer(string clientId, int fragmentIndex)
    {
        if (!playerTaskStates.TryGetValue(clientId, out var state) || state.isTaskCompleted) return;

        // 只记录未收集过的碎片
        if (!state.collectedFragmentIds.Contains(fragmentIndex))
        {
            state.collectedFragmentIds.Add(fragmentIndex);
            Debug.Log($"玩家 {clientId} 收集了碎片 {fragmentIndex}，当前进度: {state.collectedFragmentIds.Count}/9");

            // 如果该玩家已经收集满全部碎片，通知客户端切换目标为“返回 NPC 开始游戏”
            if (state.collectedFragmentIds.Count == treasureFragments.Length)
            {
                TaskStateMessage taskStateMessage = new TaskStateMessage();
                taskStateMessage.type = "task_state";
                taskStateMessage.state = "return_to_npc";
                UnitySever.Instance?.SendMessageToClient(clientId, JsonUtility.ToJson(taskStateMessage));
            }
        }
    }

    // 向单个玩家发送消息
    private void SendMessageToClient(string clientId, string title, string content)
    {
        InteractionMessage message = new InteractionMessage
        {
            type = "interaction",
            title = title,
            content = content
        };
        UnitySever.Instance?.SendMessageToClient(clientId, JsonUtility.ToJson(message));
    }
    public void HandlePuzzleCompleted(string clientId)
    {
        if (!playerTaskStates.TryGetValue(clientId, out var state)) return;

        state.isPuzzleCompleted = true; // 标记拼图完成
        state.isTaskCompleted = true; // 标记任务最终完成
        Debug.Log($"玩家 {clientId} 完成拼图，任务最终完成");
    }
}