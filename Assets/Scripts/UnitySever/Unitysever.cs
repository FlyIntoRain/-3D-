using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;
using WebSocketSharp.Server;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using TMPro;

public class UnitySever : MonoBehaviour
{
    // WebSocket服务器配置
    private WebSocketServer wssv;
    public int port = 8888;
    private bool isServerRunning = false;
    // 替换原有单个预制体为预制体数组（用于随机生成）
    public GameObject[] playerPrefabs;
    public GameObject spawnPos;
    public GameObject thirdPersonGroup;
    public GameObject firstPersonGroup;
    private bool isThirdPersonActive = true;
    public int totalPlayerCount = 0; // 用于记录历史总玩家数，初始为0

    // 名字显示相关配置（新增）
    public GameObject playerNameTagPrefab;
    public float nameTagOffsetY = 2f; // 调整为合理偏移量

    public Dictionary<string, GameObject> connectedClients = new Dictionary<string, GameObject>();
    // 存储玩家名字（新增）
    public Dictionary<string, string> playerNames = new Dictionary<string, string>();

    // 单例模式
    private static UnitySever _instance;
    public static UnitySever Instance
    {
        get { return _instance; }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Start()
    {
        // 启动WebSocket服务器
        StartWebSocketServer();
        SwitchToThirdPerson();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleCameraMode();
        }
    }

    private void StartWebSocketServer()
    {
        try
        {
            // 创建WebSocket服务器
            wssv = new WebSocketServer(port);

            // 添加WebSocket行为
            wssv.AddWebSocketService<GameWebSocketBehavior>("/");

            // 启动服务器
            wssv.Start();
            isServerRunning = true;

            Debug.Log($"WebSocket服务器已启动，监听端口: {port}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"启动WebSocket服务器失败: {ex.Message}");
            isServerRunning = false;
        }
    }

    private void StopWebSocketServer()
    {
        if (wssv != null && wssv.IsListening)
        {
            wssv.Stop();
            isServerRunning = false;
            Debug.Log("WebSocket服务器已停止");
        }
    }

    public new void BroadcastMessage(string message)
    {
        if (isServerRunning && wssv != null)
        {
            try
            {
                // 向所有连接的客户端广播消息
                wssv.WebSocketServices["/"].Sessions.Broadcast(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"广播消息失败: {ex.Message}");
            }
        }
    }

    // 向特定客户端发消息的方法
    public void SendMessageToClient(string clientId, string message)
    {
        if (isServerRunning && wssv != null)
        {
            try
            {
                Debug.Log($"向客户端 {clientId} 发送消息: {message}");
                wssv.WebSocketServices["/"].Sessions.SendTo(message, clientId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"向客户端 {clientId} 发送消息失败: {ex.Message}");
            }
        }
    }

    private void OnApplicationQuit()
    {
        // 应用退出时停止服务器
        StopWebSocketServer();
    }

    void ToggleCameraMode()
    {
        // 切换状态标记
        isThirdPersonActive = !isThirdPersonActive;

        if (isThirdPersonActive)
        {
            SwitchToThirdPerson();
        }
        else
        {
            SwitchToFirstPerson();
        }
    }

    void SwitchToThirdPerson()
    {
        // 激活第三人称组，隐藏第一人称组
        if (thirdPersonGroup != null) thirdPersonGroup.SetActive(true);
        if (firstPersonGroup != null) firstPersonGroup.SetActive(false);
        Debug.Log("切换到：第三人称 (Display 1-4)");
    }

    void SwitchToFirstPerson()
    {
        // 激活第一人称组，隐藏第三人称组
        if (thirdPersonGroup != null) thirdPersonGroup.SetActive(false);
        if (firstPersonGroup != null) firstPersonGroup.SetActive(true);
        Debug.Log("切换到：第一人称 (Display 1-4)");
    }

    // 新增：随机获取玩家预制体（核心随机生成逻辑）
    private GameObject GetRandomPlayerPrefab()
    {
        if (playerPrefabs == null || playerPrefabs.Length == 0)
        {
            Debug.LogError("玩家预制体数组为空！请在Inspector中赋值");
            return null;
        }

        // 随机生成索引，获取预制体
        int randomIndex = UnityEngine.Random.Range(0, playerPrefabs.Length);
        GameObject randomPrefab = playerPrefabs[randomIndex];
        Debug.Log($"随机选中玩家预制体：{randomPrefab.name}，索引：{randomIndex}");
        return randomPrefab;
    }

    // 新增：清理玩家对象下所有的PlayerNameTag（包括重复的，核心解决重复问题）
    private void ClearAllPlayerNameTags(Transform parentTransform)
    {
        if (parentTransform == null)
        {
            return;
        }

        // 查找所有包含PlayerNameTag的子物体（递归查找）
        List<Transform> tagsToDelete = new List<Transform>();
        FindAllNameTagsRecursive(parentTransform, "PlayerNameTag", tagsToDelete);

        // 遍历删除所有找到的标签
        foreach (Transform tagTransform in tagsToDelete)
        {
            Destroy(tagTransform.gameObject);
            Debug.Log($"已清理重复的名字标签：{tagTransform.name}");
        }
    }

    // 辅助方法：递归查找所有包含目标名称的标签
    private void FindAllNameTagsRecursive(Transform parent, string targetName, List<Transform> resultList)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(targetName))
            {
                resultList.Add(child);
            }
            // 递归查找子物体的子物体
            FindAllNameTagsRecursive(child, targetName, resultList);
        }
    }

    // 创建名字标签（优化后：强制统一名称，避免查找冲突）
    private GameObject CreatePlayerNameTag(Transform parent, string playerName)
    {
        if (playerNameTagPrefab == null)
        {
            Debug.LogError("玩家名字标签预制体未赋值！");
            return null;
        }

        // 实例化名字标签
        GameObject nameTag = Instantiate(playerNameTagPrefab, parent);
        // 强制设置标签名称为固定值，移除(Clone)后缀，便于后续精准查找
        nameTag.name = "PlayerNameTag";
        // 设置位置和旋转
        nameTag.transform.localPosition = new Vector3(0, nameTagOffsetY, 0);
        nameTag.transform.localRotation = Quaternion.identity;
        nameTag.transform.Rotate(0, 180, 0);
        nameTag.transform.localScale = Vector3.one;

        // 直接调用文本更新方法，确保创建时就赋值
        UpdatePlayerNameTagText(nameTag.transform, playerName);

        Debug.Log($"成功创建玩家名字标签，父物体：{parent.name}，玩家名字：{playerName}");
        return nameTag;
    }

    // 设置玩家名字（核心优化：优先更新已有标签，不重复创建）
    public void SetPlayerName(string clientId, string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        MainThreadDispatcher.Instance.Enqueue(() =>
        {
            // 更新名字字典
            if (playerNames.ContainsKey(clientId))
            {
                playerNames[clientId] = name;
            }
            else
            {
                playerNames.Add(clientId, name);
            }

            if (connectedClients.TryGetValue(clientId, out GameObject playerObject))
            {
                playerObject.name = name;
                // 重点1：先查找已有标签（支持模糊匹配，兼容可能的后缀）
                Transform nameTagTransform = FindChildRecursive(playerObject.transform, "PlayerNameTag");
                if (nameTagTransform != null)
                {
                    // 找到已有标签，直接更新文本，不创建新标签
                    UpdatePlayerNameTagText(nameTagTransform, name);
                }
                else
                {
                    // 未找到标签时，先清理可能的残留（防止异常堆积），再创建新标签
                    ClearAllPlayerNameTags(playerObject.transform);
                    CreatePlayerNameTag(playerObject.transform, name);
                }
            }
        });
    }

    // 专门用于更新名字标签的文本内容（不修改物体名称，兼容所有文本类型）
    private void UpdatePlayerNameTagText(Transform nameTagTransform, string newPlayerName)
    {
        if (nameTagTransform == null || string.IsNullOrEmpty(newPlayerName))
        {
            Debug.LogWarning("名字标签物体为空或新名字无效，无法更新文本");
            return;
        }

        // 1. 优先查找UI Text组件（UGUI）
        Text uiText = nameTagTransform.GetComponentInChildren<Text>(true);
        if (uiText != null)
        {
            uiText.text = newPlayerName;
            uiText.enabled = true;
            Debug.Log($"成功更新UI Text组件，新名字：{newPlayerName}");
            return;
        }

        // 2. 查找UI TextMeshPro组件（UGUI TMP）
        TextMeshProUGUI uiTmpro = nameTagTransform.GetComponentInChildren<TextMeshProUGUI>(true);
        if (uiTmpro != null)
        {
            uiTmpro.text = newPlayerName;
            uiTmpro.enabled = true;
            Debug.Log($"成功更新TextMeshProUGUI组件，新名字：{newPlayerName}");
            return;
        }

        // 3. 查找3D TextMeshPro组件（场景3D文本）
        TextMeshPro tmpro3D = nameTagTransform.GetComponentInChildren<TextMeshPro>(true);
        if (tmpro3D != null)
        {
            tmpro3D.text = newPlayerName;
            tmpro3D.enabled = true;
            Debug.Log($"成功更新3D TextMeshPro组件，新名字：{newPlayerName}");
            return;
        }

        Debug.LogError($"在名字标签 {nameTagTransform.name} 中未找到任何支持的文本组件（Text/TextMeshProUGUI/TextMeshPro）");
    }

    private string GetValueFromJson(string json, string key)
    {
        try
        {
            // 使用正则表达式提取值
            Match match = Regex.Match(json, $"\"{key}\":\\s*\"([^\"]+)\"");
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }
        catch
        {
            // 忽略解析错误
        }
        return null;
    }
    private string GetMessageType(string json)
    {
        return GetValueFromJson(json, "type");
    }


    // 递归查找子物体（支持多层嵌套+模糊匹配，解决查找失败问题）
    private Transform FindChildRecursive(Transform parentTransform, string targetChildName)
    {
        if (parentTransform == null || string.IsNullOrEmpty(targetChildName))
        {
            return null;
        }

        // 遍历当前父物体的直接子物体
        foreach (Transform childTransform in parentTransform)
        {
            // 关键：使用Contains模糊匹配，支持 PlayerNameTag(Clone) 或自定义后缀
            if (childTransform.name.Contains(targetChildName))
            {
                return childTransform;
            }

            // 递归查找子物体的子物体（支持多层嵌套）
            Transform grandChildTransform = FindChildRecursive(childTransform, targetChildName);
            if (grandChildTransform != null)
            {
                return grandChildTransform;
            }
        }

        // 未找到目标物体
        return null;
    }


    // WebSocket行为类，处理客户端连接和消息
    public class GameWebSocketBehavior : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            Debug.Log("移动端客户端已连接");
            Debug.Log($"移动端客户端已连接, Session ID: {ID}");
            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                // 随机获取玩家预制体
                GameObject selectedPrefab = UnitySever.Instance.GetRandomPlayerPrefab();
                if (selectedPrefab != null)
                {
                    GameObject newPlayer = Instantiate(selectedPrefab, UnitySever.Instance.spawnPos.GetComponent<Transform>().position, Quaternion.identity);
                    UnitySever.Instance.totalPlayerCount++;

                    // 2. 拼接名字： "玩家" + 数字
                    string defaultName = "玩家" + UnitySever.Instance.totalPlayerCount;
                    newPlayer.name = defaultName;

                    // 将新玩家与客户端ID关联起来
                    UnitySever.Instance.connectedClients.Add(ID, newPlayer);
                    UnitySever.Instance.playerNames.Add(ID, defaultName);

                    // 核心修改1：先清理所有残留标签（即使预制体无自带标签，也防止异常重复）
                    UnitySever.Instance.ClearAllPlayerNameTags(newPlayer.transform);
                    // 核心修改2：只创建1个唯一的名字标签
                    UnitySever.Instance.CreatePlayerNameTag(newPlayer.transform, defaultName);

                    Debug.Log($"为ID {ID} 创建了玩家: {newPlayer.name}, 使用随机预制体: {selectedPrefab.name}");
                }
                else
                {
                    Debug.LogError("Player Prefab 数组未在 UnitySever 中设置！");
                }

            });
            // 发送欢迎消息给客户端
            Send("{\"type\":\"connected\",\"message\":\"成功连接到Unity服务器\"}");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                string message = e.Data;
                // 确保玩家已关联（获取当前客户端ID对应的玩家对象）
                if (!UnitySever.Instance.connectedClients.TryGetValue(ID, out GameObject playerObject))
                {
                    Debug.LogWarning($"收到来自未关联玩家的客户端消息, ID: {ID}");
                    return;
                }
                // 处理名字修改消息（change_name / update_name）
                string messageType = UnitySever.Instance.GetMessageType(message);
                if (messageType == "change_name" || message.Contains("\"type\":\"change_name\""))
                {
                    string newName = UnitySever.Instance.GetValueFromJson(message, "name");
                    if (!string.IsNullOrEmpty(newName))
                    {
                        UnitySever.Instance.SetPlayerName(ID, newName);
                        Debug.Log($"玩家 {ID} 更新名字为: {newName}");
                        // 发送确认消息给客户端
                        Send($"{{\"type\":\"name_changed\",\"status\":\"success\",\"name\":\"{newName}\"}}");
                    }
                    else
                    {
                        // 发送错误消息给客户端
                        Send("{\"type\":\"name_changed\",\"status\":\"error\",\"message\":\"名字不能为空\"}");
                    }
                    return;
                }
                // 保持原有update_name兼容
                if (messageType == "update_name" || message.Contains("\"type\":\"update_name\""))
                {
                    string newName = UnitySever.Instance.GetValueFromJson(message, "name");
                    if (!string.IsNullOrEmpty(newName))
                    {
                        UnitySever.Instance.SetPlayerName(ID, newName);
                        Debug.Log($"玩家 {ID} 更新名字为: {newName}");
                    }
                    return;
                }

                if (message.Contains("\"type\":\"move\""))
                {
                    // 移动逻辑
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (UnitySever.Instance.connectedClients.TryGetValue(ID, out GameObject player))
                        {
                            ActorMove moveScript = player.GetComponent<ActorMove>();
                            Debug.Log($"处理玩家 {ID} 的移动命令: {message}");
                            if (moveScript != null)
                            {
                                moveScript.ProcessMoveCommand(message);
                            }
                        }
                    });
                }
                // 处理玩家点击NPC的交互确认
                else if (message.Contains("\"type\":\"confirm_interaction\""))
                {
                    Debug.Log($"玩家 {ID} 请求与NPC交互");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        var npc = FindObjectOfType<NPCInteraction>();
                        if (npc != null)
                        {
                            npc.HandleConfirmation(ID);
                        }
                        else
                        {
                            Debug.LogError("未找到NPCInteraction组件！");
                        }
                    });
                }
                // 处理玩家点击对话框的“确定”按钮（接受任务）
                else if (message.Contains("\"type\":\"dialog_confirm\""))
                {
                    Debug.Log($"玩家 {ID} 确认接受任务");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        var npc = FindObjectOfType<NPCInteraction>();
                        if (npc != null)
                        {
                            npc.HandleDialogResult(ID, true);
                        }
                    });
                }
                // 处理玩家点击对话框的“取消”按钮（拒绝任务）
                else if (message.Contains("\"type\":\"dialog_cancel\""))
                {
                    Debug.Log($"玩家 {ID} 取消任务");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        var npc = FindObjectOfType<NPCInteraction>();
                        if (npc != null)
                        {
                            npc.HandleDialogResult(ID, false);
                        }
                    });
                }
                // 处理移动端发来的“自动追踪”消息
                else if (message.Contains("\"type\":\"auto_npc_track\""))
                {
                    Debug.Log($"玩家 {ID} 请求自动追踪");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        // 1. 找到该玩家对应的对象
                        if (!UnitySever.Instance.connectedClients.TryGetValue(ID, out GameObject player))
                        {
                            Debug.LogError($"未找到客户端 {ID} 对应的玩家对象，无法执行自动追踪。");
                            return;
                        }

                        // 2. 获取该玩家身上的 AutoNavigation 组件
                        var autoNav = player.GetComponent<AutoNavigation>();
                        if (autoNav == null)
                        {
                            Debug.LogWarning($"玩家 {player.name} 上未找到 AutoNavigation 组件，无法自动追踪。");
                            return;
                        }

                        // 3. 获取 NPCInteraction，用于读取该玩家的任务 / 碎片状态
                        var npc = GameObject.FindObjectOfType<NPCInteraction>();
                        if (npc == null)
                        {
                            Debug.LogError("场景中未找到 NPCInteraction，无法根据任务状态决定追踪目标。");
                            return;
                        }

                        // 4. 读取该玩家的任务状态
                        if (!npc.playerTaskStates.TryGetValue(ID, out var state) || !state.isTaskStarted)
                        {
                            // 任务尚未开始：追踪 NPC 与其对话（npc1）
                            GameObject npcTarget = GameObject.Find("npc1 (1)");
                            if (npcTarget == null)
                            {
                                Debug.LogError("未在场景中找到名为 \"npc1\" 的目标对象，自动追踪失败。");
                                return;
                            }

                            autoNav.StartAutoNavigationExternal(npcTarget);
                            Debug.Log("自动追踪：前往 NPC（npc1）开始任务。");
                            return;
                        }

                        // 5. 任务已开始但碎片未集齐：按 0/1/2 顺序寻找尚未收集的碎片
                        if (state.collectedFragmentIds.Count < npc.treasureFragments.Length)
                        {
                            int nextIndex = -1;
                            for (int i = 0; i < npc.treasureFragments.Length; i++)
                            {
                                if (!state.collectedFragmentIds.Contains(i))
                                {
                                    nextIndex = i;
                                    break;
                                }
                            }

                            if (nextIndex >= 0)
                            {
                                GameObject fragmentTarget = null;

                                // 优先使用 NPCInteraction 中绑定的碎片引用
                                if (npc.treasureFragments != null &&
                                    nextIndex < npc.treasureFragments.Length)
                                {
                                    fragmentTarget = npc.treasureFragments[nextIndex];
                                }

                                // 如果数组里没配，尝试按名字查找（01、02、03 ...）
                                if (fragmentTarget == null)
                                {
                                    string fragName = (nextIndex + 1).ToString("00"); // 0->"01"
                                    fragmentTarget = GameObject.Find(fragName);
                                }

                                if (fragmentTarget == null)
                                {
                                    Debug.LogError($"未找到索引为 {nextIndex} 的碎片目标（也未找到命名为 {(nextIndex + 1).ToString("00")} 的对象），自动追踪失败。");
                                    return;
                                }

                                autoNav.StartAutoNavigationExternal(fragmentTarget);
                                Debug.Log($"自动追踪：前往碎片 {nextIndex} 对应的目标。");
                                return;
                            }
                        }

                        // 6. 碎片已集齐：追踪 NPC（npc1）去交任务 / 开始游戏
                        GameObject finalNpcTarget = GameObject.Find("npc1 (1)");
                        if (finalNpcTarget == null)
                        {
                            Debug.LogError("未在场景中找到名为 \"npc1\" 的目标对象，自动追踪失败。");
                            return;
                        }

                        autoNav.StartAutoNavigationExternal(finalNpcTarget);
                        Debug.Log("自动追踪：碎片已集齐，前往 NPC（npc1）开始游戏。");

                        // 额外：同步更新当前客户端的任务目标文字
                        try
                        {
                            var taskStateMessage = new
                            {
                                type = "task_state",
                                state = "return_to_npc"
                            };
                            string json = JsonUtility.ToJson(taskStateMessage);
                            UnitySever.Instance.SendMessageToClient(ID, json);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"发送任务状态 task_state:return_to_npc 失败: {ex.Message}");
                        }
                    });
                }
                // 处理拼图完成消息
                else if (message.Contains("\"type\":\"puzzle_completed\""))
                {
                    Debug.Log($"玩家 {ID} 完成了拼图游戏");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        HandlePuzzleCompleted(ID);
                    });
                }
                //新增：处理点击查看文物按钮
                else if (message.Contains("\"type\":\"Artifact_view\""))
                {
                    Debug.Log($"玩家 {ID} 点击了查看文物按钮");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        HandlePuzzleCompleted(ID);
                    });
                }
                //新增
                else if (message.Contains("\"type\":\"view_fragment\""))
                {
                    Debug.Log($"玩家 {ID} 从背包查看碎片");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        HandleViewFragment(ID, message);
                    });
                }
                // ============== 新增：处理碎片旋转控制消息 ==============
                else if (message.Contains("\"type\":\"fragment_model_control\""))
                {
                    Debug.Log($"玩家 {ID} 旋转碎片模型");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        HandleFragmentModelControl(ID, message);
                    });
                }// ✅ 新增：处理关闭碎片展示消息
                else if (message.Contains("\"type\":\"close_fragment_view\""))
                {
                    Debug.Log($"玩家 {ID} 关闭了碎片展示页面");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        HandleCloseFragmentView(ID);
                    });
                }
                // 处理模型控制消息（旋转）
                else if (message.Contains("\"type\":\"model_control\""))
                {
                    Debug.Log($"玩家 {ID} 旋转文物模型");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        HandleModelControl(ID, message);
                    });
                }
                // 处理关闭文物展示页面消息
                else if (message.Contains("\"type\":\"close_artifact_view\""))
                {
                    Debug.Log($"玩家 {ID} 关闭了文物展示页面");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        HandleCloseArtifactView(ID);
                    });
                }
                // 处理手机端时间/天气切换消息
                else if (message.Contains("\"type\":\"time_weather_change\""))
                {
                    Debug.Log($"手机端请求切换时间/天气：{message}");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        // 获取 WeatherTimeSystem 实例
                        WeatherTimeSystem weatherTimeSystem = FindObjectOfType<WeatherTimeSystem>();
                        if (weatherTimeSystem == null)
                        {
                            Debug.LogError("未找到 WeatherTimeSystem 实例！");
                            return;
                        }

                        // 解析时间和天气类型
                        string timeType = ParseTimeType(message);
                        string weatherType = ParseWeatherType(message);

                        // 调用手机端专属控制方法
                        weatherTimeSystem.SetTimeWeatherFromMobile(timeType, weatherType);

                        // 发送确认消息给手机端
                        string ackMessage = $"{{\"type\":\"time_weather_ack\",\"status\":\"success\",\"time\":\"{timeType}\",\"weather\":\"{weatherType}\"}}";
                        UnitySever.Instance.SendMessageToClient(ID, ackMessage);
                    });
                }
                // 处理自动模式切换消息（新增）
                else if (message.Contains("\"type\":\"mode_change\""))
                {
                    Debug.Log($"收到自动模式切换指令：{message}");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        WeatherTimeSystem weatherTimeSystem = FindObjectOfType<WeatherTimeSystem>();
                        if (weatherTimeSystem == null)
                        {
                            Debug.LogError("未找到 WeatherTimeSystem 实例，无法切换自动模式！");
                            return;
                        }

                        // 解析模式（auto/manual）
                        string mode = ParseModeType(message);
                        bool isAutoMode = mode.Equals("auto", StringComparison.OrdinalIgnoreCase);
                        weatherTimeSystem.EnableAutoMode(isAutoMode);

                        // 发送确认消息给前端
                        string ackMessage = $"{{\"type\":\"mode_ack\",\"mode\":\"{mode}\",\"status\":\"success\"}}";
                        UnitySever.Instance.SendMessageToClient(ID, ackMessage);
                    });
                }
                else
                {
                    Debug.Log($"未处理的消息类型: {message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理消息失败: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
            }
        }

        // ============== 新增：处理碎片相关的方法 ==============

        // 处理查看碎片请求
        private void HandleViewFragment(string clientId, string message)
        {
            try
            {
                Debug.Log($"=== 处理玩家 {clientId} 的查看碎片请求 ===");
                Debug.Log($"原始消息: {message}");

                if (UnitySever.Instance.connectedClients.TryGetValue(clientId, out GameObject player))
                {
                    Debug.Log($"找到玩家对象: {player.name}");

                    // 解析碎片信息
                    string fragmentId = ParseFragmentIdFromMessage(message);
                    Debug.Log($"解析到碎片ID: {fragmentId}");

                    if (!string.IsNullOrEmpty(fragmentId))
                    {
                        // 在玩家上方显示碎片
                        ShowFragmentAbovePlayer(player, fragmentId);

                        // 发送确认消息
                        SendFragmentViewAck(clientId, fragmentId, true);
                    }
                    else
                    {
                        Debug.LogWarning("无法解析碎片ID");
                        SendFragmentViewAck(clientId, "unknown", false);
                    }
                }
                else
                {
                    Debug.LogError($"未找到客户端 {clientId} 对应的玩家对象");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理查看碎片请求失败: {ex.Message}\n{ex.StackTrace}");
                SendFragmentViewAck(clientId, "error", false, ex.Message);
            }
        }

        // 处理关闭碎片展示
        private void HandleCloseFragmentView(string clientId)
        {
            try
            {
                Debug.Log($"=== 处理关闭碎片展示，客户端ID: {clientId} ===");

                if (UnitySever.Instance.connectedClients.TryGetValue(clientId, out GameObject player))
                {
                    Debug.Log($"找到玩家对象: {player.name}");

                    FragmentController fragmentController = player.GetComponent<FragmentController>();
                    if (fragmentController == null)
                    {
                        Debug.LogWarning($"玩家 {player.name} 没有 FragmentController 组件");
                        return;
                    }

                    if (fragmentController.IsFragmentVisible())
                    {
                        fragmentController.ForceDestroyFragment(); // 使用新的强制隐藏方法
                        Debug.Log($"成功隐藏玩家 {clientId} 的碎片模型");

                        // 发送确认消息
                        string confirmMessage = "{\"type\":\"fragment_hidden\",\"message\":\"碎片已隐藏\"}";
                        UnitySever.Instance.SendMessageToClient(clientId, confirmMessage);
                    }
                    else
                    {
                        Debug.Log($"玩家 {clientId} 的碎片本来就不可见");
                    }
                }
                else
                {
                    Debug.LogError($"未找到客户端 {clientId} 对应的玩家对象");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"关闭碎片展示失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // 处理碎片旋转控制
        private void HandleFragmentModelControl(string clientId, string message)
        {
            try
            {
                Debug.Log($"收到碎片模型控制消息: {message}");

                if (UnitySever.Instance.connectedClients.TryGetValue(clientId, out GameObject player))
                {
                    // 查找玩家的FragmentController
                    FragmentController fragmentController = player.GetComponent<FragmentController>();
                    if (fragmentController != null && fragmentController.IsFragmentVisible())
                    {
                        // 解析旋转角度
                        float rotationY = ParseRotationY(message);

                        // 应用旋转
                        fragmentController.SetRotation(rotationY);
                        Debug.Log($"设置碎片旋转到: {rotationY} 弧度");

                        // 发送确认消息
                        SendFragmentControlAck(clientId, rotationY);
                    }
                    else
                    {
                        Debug.LogWarning($"玩家 {clientId} 没有可见的碎片，无法应用旋转");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理碎片模型控制失败: {ex.Message}");
            }
        }

        // 在玩家上方显示碎片
        private void ShowFragmentAbovePlayer(GameObject player, string fragmentId)
        {
            try
            {
                // 获取或添加FragmentController组件
                FragmentController fragmentController = player.GetComponent<FragmentController>();
                if (fragmentController == null)
                {
                    fragmentController = player.AddComponent<FragmentController>();
                    Debug.Log($"为玩家 {player.name} 添加FragmentController组件");
                }

                // 显示碎片
                fragmentController.ShowFragment(fragmentId);
                Debug.Log($"为玩家 {player.name} 显示碎片: {fragmentId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"显示碎片失败: {ex.Message}");
            }
        }

        // 解析碎片ID
        private string ParseFragmentIdFromMessage(string json)
        {
            try
            {
                int startIndex = json.IndexOf("\"fragmentId\":\"");
                if (startIndex == -1)
                {
                    // 尝试 "id" 字段
                    startIndex = json.IndexOf("\"id\":\"");
                    if (startIndex == -1)
                    {
                        Debug.LogWarning("消息中未找到fragmentId或id字段");
                        return null;
                    }
                    startIndex += "\"id\":\"".Length;
                }
                else
                {
                    startIndex += "\"fragmentId\":\"".Length;
                }

                int endIndex = json.IndexOf("\"", startIndex);
                if (endIndex == -1)
                {
                    Debug.LogWarning("碎片ID字符串格式错误");
                    return null;
                }

                return json.Substring(startIndex, endIndex - startIndex).Trim();
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析碎片ID失败: {ex.Message}");
                return null;
            }
        }

        // 发送碎片查看确认消息
        private void SendFragmentViewAck(string clientId, string fragmentId, bool success, string errorMsg = "")
        {
            try
            {
                var ackMessage = new
                {
                    type = "fragment_view_ack",
                    fragmentId = fragmentId,
                    status = success ? "success" : "error",
                    message = success ? "碎片已显示" : $"显示失败: {errorMsg}",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string json = JsonUtility.ToJson(ackMessage);
                UnitySever.Instance.SendMessageToClient(clientId, json);
                Debug.Log($"已向客户端 {clientId} 发送碎片查看确认");
            }
            catch (Exception ex)
            {
                Debug.LogError($"发送碎片查看确认失败: {ex.Message}");
            }
        }

        // 发送碎片控制确认消息
        private void SendFragmentControlAck(string clientId, float rotationY)
        {
            try
            {
                var ackMessage = new
                {
                    type = "fragment_control_ack",
                    rotation_y = rotationY,
                    status = "success",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string json = JsonUtility.ToJson(ackMessage);
                UnitySever.Instance.SendMessageToClient(clientId, json);
                Debug.Log($"已向客户端 {clientId} 发送碎片控制确认");
            }
            catch (Exception ex)
            {
                Debug.LogError($"发送碎片控制确认失败: {ex.Message}");
            }
        }
        // ============== 新增方法结束 ==============

        // 解析时间类型
        private string ParseTimeType(string json)
        {
            try
            {
                int startIndex = json.IndexOf("\"time\":\"");
                if (startIndex == -1)
                {
                    Debug.LogError("时间消息中未找到\"time\"字段");
                    return null;
                }

                startIndex += "\"time\":\"".Length;
                int endIndex = json.IndexOf("\"", startIndex);
                if (endIndex == -1)
                {
                    Debug.LogError("时间类型字符串格式错误");
                    return null;
                }

                return json.Substring(startIndex, endIndex - startIndex).Trim();
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析时间类型失败：{ex.Message}");
                return null;
            }
        }

        // 解析天气类型
        private string ParseWeatherType(string json)
        {
            try
            {
                int startIndex = json.IndexOf("\"weather\":\"");
                if (startIndex == -1)
                {
                    Debug.LogError("天气消息中未找到\"weather\"字段（检查手机端消息格式）");
                    return null;
                }

                startIndex += "\"weather\":\"".Length;
                int endIndex = json.IndexOf("\"", startIndex);
                if (endIndex == -1)
                {
                    Debug.LogError("天气类型字符串格式错误（引号未闭合）");
                    return null;
                }

                string weatherType = json.Substring(startIndex, endIndex - startIndex).Trim();
                Debug.Log($"解析到手机端天气类型：{weatherType}");
                return weatherType;
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析天气类型失败：{ex.Message}");
                return null;
            }
        }

        // 新增：解析模式类型（auto/manual）
        private string ParseModeType(string json)
        {
            try
            {
                int startIndex = json.IndexOf("\"mode\":\"");
                if (startIndex == -1)
                {
                    Debug.LogError("模式消息中未找到\"mode\"字段");
                    return "manual"; // 默认手动模式
                }

                startIndex += "\"mode\":\"".Length;
                int endIndex = json.IndexOf("\"", startIndex);
                if (endIndex == -1)
                {
                    Debug.LogError("模式类型字符串格式错误");
                    return "manual";
                }

                return json.Substring(startIndex, endIndex - startIndex).Trim().ToLower();
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析模式类型失败：{ex.Message}");
                return "manual";
            }
        }

        // 处理拼图完成（原有）
        private void HandlePuzzleCompleted(string clientId)
        {
            try
            {
                Debug.Log($"处理玩家 {clientId} 的拼图完成事件");

                if (UnitySever.Instance.connectedClients.TryGetValue(clientId, out GameObject player))
                {
                    var npc = FindObjectOfType<NPCInteraction>();
                    npc?.HandlePuzzleCompleted(clientId);

                    // 在玩家上方显示文物模型
                    ShowArtifactAbovePlayer(player);

                    // 发送确认消息
                    SendPuzzleCompletedAck(clientId);
                }
                else
                {
                    Debug.LogError($"未找到客户端 {clientId} 对应的玩家");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理拼图完成事件失败: {ex.Message}");
            }
        }

        // 处理模型控制（原有）
        private void HandleModelControl(string clientId, string message)
        {
            try
            {
                Debug.Log($"收到模型控制消息: {message}");

                if (UnitySever.Instance.connectedClients.TryGetValue(clientId, out GameObject player))
                {
                    ArtifactController artifactController = player.GetComponent<ArtifactController>();
                    if (artifactController != null && artifactController.IsArtifactVisible())
                    {
                        float rotationY = ParseRotationY(message);
                        if (rotationY != float.MinValue)
                        {
                            artifactController.SetRotation(rotationY);
                            Debug.Log($"设置文物旋转到: {rotationY} 弧度");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理模型控制失败: {ex.Message}");
            }
        }

        // 发送拼图确认消息（原有）
        private void SendPuzzleCompletedAck(string clientId)
        {
            try
            {
                string jsonResponse = "{\"type\":\"puzzle_completed_ack\",\"message\":\"拼图完成确认，文物已显示\"}";
                Send(jsonResponse);
                Debug.Log($"已向客户端 {clientId} 发送确认消息");
            }
            catch (Exception ex)
            {
                Debug.LogError($"发送确认消息失败: {ex.Message}");
            }
        }

        // 解析旋转角度（原有）
        private float ParseRotationY(string json)
        {
            try
            {
                int startIndex = json.IndexOf("\"rotation_y\":");
                if (startIndex == -1)
                {
                    startIndex = json.IndexOf("\"y\":");
                    if (startIndex == -1) return float.MinValue;

                    int endIndex = json.IndexOf(",", startIndex);
                    if (endIndex == -1) endIndex = json.IndexOf("}", startIndex);

                    if (endIndex > startIndex)
                    {
                        string valueStr = json.Substring(startIndex + 4, endIndex - (startIndex + 4)).Trim();
                        if (float.TryParse(valueStr, out float value))
                        {
                            return value;
                        }
                    }
                }
                else
                {
                    startIndex += 13;
                    int endIndex = json.IndexOf(",", startIndex);
                    if (endIndex == -1) endIndex = json.IndexOf("}", startIndex);

                    if (endIndex > startIndex)
                    {
                        string valueStr = json.Substring(startIndex, endIndex - startIndex).Trim();
                        if (float.TryParse(valueStr, out float value))
                        {
                            return value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析旋转Y失败: {ex.Message}");
            }

            return float.MinValue;
        }

        // 处理关闭文物展示（原有）
        private void HandleCloseArtifactView(string clientId)
        {
            try
            {
                Debug.Log($"=== 处理关闭文物展示，客户端ID: {clientId} ===");

                if (UnitySever.Instance.connectedClients.TryGetValue(clientId, out GameObject player))
                {
                    Debug.Log($"找到玩家对象: {player.name}");

                    ArtifactController artifactController = player.GetComponent<ArtifactController>();
                    if (artifactController == null)
                    {
                        Debug.LogWarning($"玩家 {player.name} 没有 ArtifactController 组件");
                        artifactController = player.AddComponent<ArtifactController>();
                    }

                    if (artifactController.IsArtifactVisible())
                    {
                        artifactController.HideArtifact();
                        Debug.Log($"成功隐藏玩家 {clientId} 的文物模型");

                        // 发送确认消息
                        Send("{\"type\":\"artifact_hidden\",\"message\":\"文物已隐藏\"}");
                    }
                    else
                    {
                        Debug.Log($"玩家 {clientId} 的文物本来就不可见");
                    }
                }
                else
                {
                    Debug.LogError($"未找到客户端 {clientId} 对应的玩家对象");
                    Debug.Log($"当前连接客户端数: {UnitySever.Instance.connectedClients.Count}");

                    foreach (var kvp in UnitySever.Instance.connectedClients)
                    {
                        Debug.Log($"客户端: {kvp.Key} -> 玩家: {kvp.Value?.name ?? "null"}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"关闭文物展示失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // 显示文物（原有）
        private void ShowArtifactAbovePlayer(GameObject player)
        {
            ArtifactController artifactController = player.GetComponent<ArtifactController>();
            if (artifactController == null)
            {
                artifactController = player.AddComponent<ArtifactController>();
            }
            artifactController.ShowArtifact();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Debug.Log("移动端已断开连接");
            // 当客户端断开连接时，销毁玩家对象
            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (UnitySever.Instance.connectedClients.TryGetValue(ID, out GameObject playerObject))
                {
                    Destroy(playerObject);
                    UnitySever.Instance.connectedClients.Remove(ID);
                    Debug.Log($"ID {ID} 对应的玩家已被销毁");
                }
            });
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Debug.LogError($"WebSocket错误: {e.Message}");
        }
    }
}