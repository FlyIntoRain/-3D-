// WebSocket连接管理
let ws = null;
const serverHost = window.location.hostname;
let connectionUrl = `ws://${serverHost}:8888`;

let isConnected = false;
console.log('将要连接的WebSocket地址:', connectionUrl);
// addMessage(`准备连接到: ${connectionUrl}`);

// 游戏状态
const gameState = {
    position: { x: 0, y: 0 },
    fragmentsFound: 0,
    obtainedFragments: new Set() // 已获得的碎片ID集合
};
let artifactUnlocked = false;

// 背包系统实例（全局变量，方便在其他函数中访问）
let inventory = null;
let playerName = "Player"; // 默认玩家名称

// "开始游戏"按钮（位于对话框底部）
const dialogStartPuzzleBtn = document.getElementById('dialog-start-puzzle');

function setDialogStartPuzzleVisible(visible) {
    if (!dialogStartPuzzleBtn) return;
    if (visible) {
        dialogStartPuzzleBtn.classList.remove('hidden');
    } else {
        dialogStartPuzzleBtn.classList.add('hidden');
    }
}

// 遥杆相关变量
const joystick = {
    container: null,//外部圆圈
    thumb: null,//内部可拖动的小圆
    rect: null,
    isDragging: false,
    center: { x: 0, y: 0 },
    lastPosition: { x: 0, y: 0 },
    sendInterval: null,

    init: function () {
        this.container = document.getElementById('joystick-container');
        this.thumb = document.getElementById('joystick-thumb');
        this.rect = this.container.getBoundingClientRect();
        this.center = {
            x: this.rect.width / 2,
            y: this.rect.height / 2
        };

        // 添加触摸事件监听
        this.container.addEventListener('touchstart', this.onStart.bind(this));
        document.addEventListener('touchmove', this.onMove.bind(this));
        document.addEventListener('touchend', this.onEnd.bind(this));

        // 添加鼠标事件监听（用于调试）
        this.container.addEventListener('mousedown', this.onStart.bind(this));
        document.addEventListener('mousemove', this.onMove.bind(this));
        document.addEventListener('mouseup', this.onEnd.bind(this));

        // 设置定时发送位置更新
        this.sendInterval = setInterval(() => {
            if (isConnected && (this.lastPosition.x !== 0 || this.lastPosition.y !== 0)) {
                this.sendPosition();
            }
        }, 100); // 每100ms发送一次
    },

    onStart: function (e) {
        e.preventDefault();
        this.isDragging = true;
        this.updatePosition(e);
    },

    onMove: function (e) {
        if (this.isDragging) {
            e.preventDefault();
            this.updatePosition(e);
        }
    },

    onEnd: function () {
        this.isDragging = false;
        // 重置遥杆位置
        this.thumb.style.transform = `translate(-50%, -50%)`;
        this.lastPosition = { x: 0, y: 0 };

        // 发送停止移动命令
        if (isConnected) {
            this.sendPosition();
        }
    },

    updatePosition: function (e) {
        const touch = e.type.includes('mouse') ? e : e.touches[0];
        const rect = this.container.getBoundingClientRect();

        let x = touch.clientX - rect.left - this.center.x;
        let y = touch.clientY - rect.top - this.center.y;

        // 限制在圆圈内
        const distance = Math.sqrt(x * x + y * y);
        const radius = this.center.x;

        if (distance > radius) {
            const angle = Math.atan2(y, x);
            x = Math.cos(angle) * radius;
            y = Math.sin(angle) * radius;
        }

        // 更新视觉位置
        this.thumb.style.transform = `translate(calc(-50% + ${x}px), calc(-50% + ${y}px))`;

        // 计算标准化的移动向量 (-1 到 1)
        this.lastPosition = {
            x: Math.round((x / radius) * 100) / 100,
            y: Math.round((y / radius) * 100) / 100
        };
    },

    sendPosition: function () {
        if (isConnected) {
            const message = {
                type: 'move',
                x: this.lastPosition.x,
                y: this.lastPosition.y
            };
            ws.send(JSON.stringify(message));
        }
    }
};

// WebSocket连接函数
function connectWebSocket() {
    try {
        ws = new WebSocket(connectionUrl);//手机ip:8888

        ws.onopen = function () {
            console.log('WebSocket连接已建立');
            isConnected = true;
            updateConnectionStatus(true);
            addMessage('已成功连接到游戏服务器');
                // 连接成功后立即发送初始名称设置请求
            if (playerName) {
                ws.send(JSON.stringify({
                    type: "change_name",
                    name: playerName
                }));
            }
        };

        

        ws.onmessage = function (event) {
            try {
                const data = JSON.parse(event.data);
                handleMessage(data);
            } catch (e) {
                console.error('解析消息失败:', e);
            }
        };

        ws.onclose = function () {
            console.log('WebSocket连接已关闭');
            isConnected = false;
            updateConnectionStatus(false);
            addMessage('连接已断开，请检查服务器');

            // 尝试重连
            setTimeout(connectWebSocket, 5000);
        };

        ws.onerror = function (error) {
            console.error('WebSocket错误:', error);
            isConnected = false;
            updateConnectionStatus(false);
        };
    } catch (e) {
        console.error('连接WebSocket失败:', e);
        setTimeout(connectWebSocket, 5000);
    }
}

// 处理收到的消息
// 在handleMessage函数中增加调试
function handleMessage(data) {
    console.log('收到服务器消息:', JSON.stringify(data)); // 打印完整消息

    switch (data.type) {
        case 'status':
            console.log('更新游戏状态:', data);
            updateGameStatus(data);
            break;
        case 'treasure_found':
            console.log('发现碎片，当前计数:', data.count, '碎片索引:', data.fragmentIndex);
            handleTreasureFound(data);
            break;
        case 'interaction':
            console.log('显示交互对话框:', data.title, data.content);
            showDialog(data.title, data.content);
            break;
        case 'npc_status':
            console.log('NPC状态更新:', '范围内:', data.inRange, '可交互:', data.canInteract);
            // 更新按钮状态的逻辑...
            break;
        case 'task_state':
            console.log('任务状态更新:', data.state);
            updateCurrentTargetLabel(data.state);
            break;
        case 'connected':
            console.log('连接状态:', data.message);
            addMessage(data.message);
            break;
        case 'name_changed': // 处理名称修改响应
            console.log('名称修改结果:', data.status);
            if (data.status === "success") {
                playerName = data.name;
                addMessage(`名称修改成功！当前名称: ${data.name}`);
            } else {
                addMessage(`名称修改失败: ${data.message}`);
            }
            break;
        default:
            console.log('未知消息类型:', data.type);
    }
}

// 根据任务状态更新"当前目标"文字
function updateCurrentTargetLabel(state) {
    const label = document.getElementById('current-target-label');
    if (!label) return;

    switch (state) {
        case 'collect_fragments':
            label.textContent = '当前目标：找寻九个碎片';
            break;
        case 'return_to_npc':
            label.textContent = '当前目标：与 NPC 对话开始游戏';
            break;
        default:
            label.textContent = '当前目标：与 NPC 对话';
            break;
    }
}

function sendInteraction(type, data = {}) {
    if (!isConnected) {
        console.log('发送失败: 未连接到服务器');
        addMessage('未连接到服务器，请稍后再试');
        return;
    }

    const message = { type: type, ...data };
    const messageStr = JSON.stringify(message);
    console.log('发送到服务器:', messageStr);
    ws.send(messageStr);
}

// 更新游戏状态
function updateGameStatus(data) {
    if (data.fragmentsFound !== undefined) {
        gameState.fragmentsFound = data.fragmentsFound;
        document.getElementById('fragment-count').textContent = `${gameState.fragmentsFound}/9`;
    }

    // 如果服务器发送了已获得的碎片列表，更新集合
    if (data.obtainedFragmentIds && Array.isArray(data.obtainedFragmentIds)) {
        gameState.obtainedFragments = new Set(data.obtainedFragmentIds);
        // 确保背包已初始化，然后同步更新背包状态
        ensureInventoryInitialized();
        if (inventory) {
            for (let i = 1; i <= 9; i++) {
                const fragmentId = `fragment${i}`;
                const obtained = gameState.obtainedFragments.has(fragmentId);
                inventory.setItemObtained(fragmentId, obtained);
            }
        }
    }
}

// 确保背包已初始化（在全局作用域定义，供多个函数调用）
function ensureInventoryInitialized() {
    if (!inventory) {
        inventory = new Inventory();

        // 设置物品点击回调
        inventory.onItemClick = function (itemId, item) {
            console.log('点击物品:', itemId, item);
            if (item.obtained) {
                addMessage(`查看物品: ${item.name}`);
            } else {
                addMessage(`未获得物品: ${item.name}`);
            }
        };

        inventory.onArtifactClick = function () {
            if (typeof showArtifactDialog === 'function') {
                showArtifactDialog();
            } else {
                showDialog('文物展示', '<p>文物展示功能暂不可用。</p>');
            }
        };

        inventory.setArtifactButtonVisible(artifactUnlocked);

        // 初始化9个文物碎片
        inventory.setItems([
            { id: 'fragment1', name: '文物碎片1', icon: 'fragment1.png', obtained: false, description: '第一块文物碎片' },
            { id: 'fragment2', name: '文物碎片2', icon: 'fragment2.png', obtained: false, description: '第二块文物碎片' },
            { id: 'fragment3', name: '文物碎片3', icon: 'fragment3.png', obtained: false, description: '第三块文物碎片' },
            { id: 'fragment4', name: '文物碎片4', icon: 'fragment4.png', obtained: false, description: '第四块文物碎片' },
            { id: 'fragment5', name: '文物碎片5', icon: 'fragment5.png', obtained: false, description: '第五块文物碎片' },
            { id: 'fragment6', name: '文物碎片6', icon: 'fragment6.png', obtained: false, description: '第六块文物碎片' },
            { id: 'fragment7', name: '文物碎片7', icon: 'fragment7.png', obtained: false, description: '第七块文物碎片' },
            { id: 'fragment8', name: '文物碎片8', icon: 'fragment8.png', obtained: false, description: '第八块文物碎片' },
            { id: 'fragment9', name: '文物碎片9', icon: 'fragment9.png', obtained: false, description: '第九块文物碎片' }
        ]);
    }
    return inventory;
}

// 处理发现碎片
function handleTreasureFound(data) {
    // 0. 确保背包已初始化（在更新状态前）
    ensureInventoryInitialized();

    // 1. 使用服务器返回的count更新本地计数（避免累加错误）
    gameState.fragmentsFound = data.count;

    // 2. 将当前获得的碎片ID添加到已获得集合中
    if (data.fragmentIndex !== undefined) {
        const fragmentId = `fragment${data.fragmentIndex + 1}`;
        gameState.obtainedFragments.add(fragmentId);
    } else if (data.fragmentId) {
        // 如果服务器直接发送fragmentId
        gameState.obtainedFragments.add(data.fragmentId);
    }

    // 如果服务器发送了完整的已获得碎片列表，使用列表更新集合
    if (data.obtainedFragmentIds && Array.isArray(data.obtainedFragmentIds)) {
        gameState.obtainedFragments = new Set(data.obtainedFragmentIds);
    }

    // 3. 更新UI显示
    document.getElementById('fragment-count').textContent = `${gameState.fragmentsFound}/9`;

    // 4. 更新背包物品状态（保留之前已获得的碎片状态）
    if (inventory) {
        for (let i = 1; i <= 9; i++) {
            const fragmentId = `fragment${i}`;
            const obtained = gameState.obtainedFragments.has(fragmentId);
            inventory.setItemObtained(fragmentId, obtained);
        }
    }

    // 5. 根据收集数量显示不同提示
    const totalFragments = 9; // 总碎片数量（根据实际情况修改）
    if (gameState.fragmentsFound < totalFragments) {
        // 未收集完：显示继续寻找的提示
        const fragmentNum = data.fragmentIndex !== undefined ? data.fragmentIndex + 1 : gameState.fragmentsFound;
        addMessage(`恭喜！你发现了第 ${fragmentNum} 块文物碎片！`);
        showDialog(
            '发现碎片！',
            `<p>你找到了第 ${fragmentNum} 块文物碎片！继续努力，寻找剩下的碎片吧！</p>`
        );
    } else {
        // 收集完所有碎片：显示返回NPC的提示
        addMessage(`恭喜！你已找到所有 ${totalFragments} 块文物碎片！`);
        updateCurrentTargetLabel('return_to_npc');
        showDialog(
            '全部找到！',
            `<p>恭喜你找到所有文物碎片！请返回NPC身边并点击确定结束任务。</p>`
        );

    }
}

// 更新连接状态显示
function updateConnectionStatus(connected) {
    const statusText = document.getElementById('status-text');
    const statusIndicator = document.getElementById('status-indicator');

    if (connected) {
        statusText.textContent = '已连接';
        statusIndicator.className = 'w-3 h-3 bg-green-500 rounded-full ml-2';
    } else {
        statusText.textContent = '未连接';
        statusIndicator.className = 'w-3 h-3 bg-red-500 rounded-full ml-2';
    }
}

// 添加消息到消息区域
function addMessage(text) {
    const messageContent = document.getElementById('message-content');
    const newMessage = document.createElement('p');
    newMessage.textContent = text;
    messageContent.appendChild(newMessage);

    // 滚动到底部
    messageContent.scrollTop = messageContent.scrollHeight;

    // 限制消息数量
    if (messageContent.children.length > 5) {
        messageContent.removeChild(messageContent.firstChild);
    }
}

// 显示对话框
function showDialog(title, content, buttons = null) {
    const overlay = document.getElementById('dialog-overlay');
    const dialogTitle = document.getElementById('dialog-title');
    const dialogContent = document.getElementById('dialog-content');
    const defaultButtons = document.querySelector('#dialog-overlay .flex.justify-end.space-x-2.flex-wrap');

    dialogTitle.textContent = title;
    dialogContent.innerHTML = content;

    // 如果提供了自定义按钮配置
    if (buttons) {
        // 清空并重建按钮容器
        defaultButtons.innerHTML = '';

        buttons.forEach(button => {
            const btn = document.createElement('button');
            btn.id = button.id;
            btn.className = button.className;
            btn.textContent = button.text;
            btn.onclick = button.onClick;
            defaultButtons.appendChild(btn);
        });
    } else {
        // 使用默认按钮
        document.getElementById('dialog-confirm').textContent = '确定';
        document.getElementById('dialog-cancel').textContent = '取消';
        document.getElementById('dialog-start-puzzle').classList.add('hidden');

    }

    setDialogStartPuzzleVisible(false);
    if (title === "任务结束") {
        setDialogStartPuzzleVisible(true);
    }
    overlay.classList.remove('hidden');

}


// 隐藏对话框
function hideDialog() {
    const overlay = document.getElementById('dialog-overlay');
    overlay.classList.add('hidden');
}

// 自动追踪按钮：向服务器发送自动导航请求
const autoTrackBtn = document.getElementById('auto-track-btn');
if (autoTrackBtn) {
    autoTrackBtn.addEventListener('click', () => {
        console.log('点击自动追踪NPC');
        // 发送给 Unity 的自动追踪指令，目标为 npc1
        sendInteraction('auto_npc_track', { target: 'npc1' });
    });
}

// ===================== 时间/天气控制核心函数 =====================
/**
 * 发送自动模式指令（Unity自动切换时间/天气）
 */
function sendAutoModeCommand() {
    if (!isConnected) {
        addMessage('未连接到服务器，无法启用自动模式');
        return;
    }

    const message = {
        type: 'mode_change',
        mode: 'auto'
    };

    ws.send(JSON.stringify(message));
    console.log('发送自动模式指令：', message);
    addMessage('已启用自动模式，时间和天气将自动切换');
}

/**
 * 发送时间+天气切换指令（手动模式）
 * @param {string} time - 选中的时间（day/dusk/night）
 * @param {string} weather - 选中的天气（clear/cloudy/rain/snow）
 */
function sendTimeWeatherCommand(time, weather) {
    if (!isConnected) {
        addMessage('未连接到服务器，无法切换时间/天气');
        return;
    }

    // 时间中文映射
    const timeNameMap = {
        day: '白天',
        dusk: '黄昏',
        night: '夜晚'
    };

    // 天气中文映射
    const weatherNameMap = {
        clear: '晴天',
        cloudy: '阴天',
        rain: '下雨',
        snow: '下雪'
    };

    // 发送WebSocket消息（需Unity端配合解析）
    const message = {
        type: 'time_weather_change',
        time: time,
        weather: weather
    };

    ws.send(JSON.stringify(message));
    console.log('发送时间/天气指令：', message);
    addMessage(`已切换到 ${timeNameMap[time]} · ${weatherNameMap[weather]}`);
}

/**
 * 处理Unity返回的时间/天气确认消息
 */
function handleTimeWeatherAck(data) {
    if (data.status === 'success') {
        const timeNameMap = { day: '白天', dusk: '黄昏', night: '夜晚' };
        const weatherNameMap = { clear: '晴天', cloudy: '阴天', rain: '下雨', snow: '下雪' };
        addMessage(`✅ 环境更新成功：${timeNameMap[data.time]} · ${weatherNameMap[data.weather]}`);
    } else {
        addMessage(`❌ 环境更新失败：${data.message}`);
    }
}

// 初始化应用
function initApp() {
    // 初始化遥杆
    joystick.init();

    // 提前初始化背包（但不显示），确保状态可以同步更新
    ensureInventoryInitialized();

    // 尝试连接WebSocket
    connectWebSocket();


    document.getElementById('confirm').addEventListener('click', () => {
        console.log('确认点击');
        sendInteraction('confirm_interaction');
    });

    document.getElementById('cancel').addEventListener('click', () => {
        console.log('取消点击');
    });//待完善

    // 绑定背包按钮事件
    document.getElementById('bag').addEventListener('click', () => {
        const inv = ensureInventoryInitialized();
        if (inv) {
            inv.toggle();
        }
    });

    // 拼图游戏实例
    let puzzleGame = null;

    // 初始化拼图游戏
    function initPuzzleGame() {
        if (!puzzleGame) {
            // 确保元素存在后再初始化
            const overlay = document.getElementById('puzzle-game-overlay');
            if (!overlay) {
                console.error('拼图游戏界面元素未找到');
                return null;
            }

            puzzleGame = new PuzzleGame();
            // 设置完成回调
            puzzleGame.onComplete = function () {
                console.log('拼图完成！');
                hidePuzzleGame();
                artifactUnlocked = true;
                if (inventory) {
                    inventory.setArtifactButtonVisible(true);
                }
                if (typeof showArtifactDialog === 'function') {
                    showArtifactDialog();
                } else {
                    showDialog('任务完成', '<p>拼图完成，文物展示功能暂不可用。</p>');
                }
                if (isConnected) {
                    sendInteraction('puzzle_completed');
                }
                addMessage('拼图完成，请在背包中查看文物');
            };
        }
        return puzzleGame;
    }

    // 显示拼图游戏界面
    function showPuzzleGame(imageUrl = null) {
        const overlay = document.getElementById('puzzle-game-overlay');
        overlay.classList.remove('hidden');

        // 延迟一下确保DOM渲染完成后再初始化游戏
        setTimeout(() => {
            // 初始化游戏
            const game = initPuzzleGame();
            if (game) {
                game.startGame(imageUrl);
            }
        }, 100);
    }

    // 隐藏拼图游戏界面
    function hidePuzzleGame() {
        const overlay = document.getElementById('puzzle-game-overlay');
        overlay.classList.add('hidden');
    }

    // 绑定拼图游戏按钮事件
    document.getElementById('puzzle-close-btn').addEventListener('click', hidePuzzleGame);
    document.getElementById('puzzle-close-btn-bottom').addEventListener('click', hidePuzzleGame);
    document.getElementById('puzzle-reset-btn').addEventListener('click', () => {
        if (puzzleGame) {
            puzzleGame.reset();
        }
    });

    if (dialogStartPuzzleBtn) {
        dialogStartPuzzleBtn.addEventListener('click', () => {
            setDialogStartPuzzleVisible(false);
            hideDialog();
            showPuzzleGame();
        });
    }

    document.getElementById('help-btn').addEventListener('click', () => {
        showDialog(
            '游戏帮助',
            `<p>使用左侧遥杆控制角色移动</p>
                                <p>尝试收集所有碎片！</p>`
        );//这里写游戏规则
        console.log('帮助点击');
    });

    // 独立的自定义改名对话框（创建新元素，不使用现有对话框）
    function showChangeNameDialogCustom() {
        // 创建或获取设置对话框的遮罩层
        let settingsOverlay = document.getElementById('change-name-overlay');

        // 如果不存在，则创建
        if (!settingsOverlay) {
            settingsOverlay = document.createElement('div');
            settingsOverlay.id = 'change-name-overlay';
            settingsOverlay.className = 'fixed inset-0 bg-black bg-opacity-50 z-60 flex items-center justify-center hidden';
            document.body.appendChild(settingsOverlay);

            // 添加点击外部关闭功能
            settingsOverlay.addEventListener('click', function (e) {
                if (e.target === settingsOverlay) {
                    hideChangeNameDialog();
                }
            });
        }

        // 设置对话框内容，保持与现有样式一致
        settingsOverlay.innerHTML = `
        <div class="panel w-[80%] max-w-md">
            <div class="p-4">
                <h3 class="text-xl font-bold mb-3 text-primary">设置</h3>
                <div class="mb-4 text-sm">
                    <p>玩家名称: <input type="text" id="player-name-input" 
                                   value="${playerName}" 
                                   class="border rounded p-1 text-xs w-full" 
                                   maxlength="12"></p>
                </div>
                <div class="flex justify-end space-x-2 flex-wrap">
                    <button id="change-name-cancel" class="btn-secondary">取消</button>
                    <button id="change-name-save" class="btn-primary">保存</button>
                </div>
            </div>
        </div>
    `;

        // 显示对话框
        settingsOverlay.classList.remove('hidden');

        // 绑定按钮事件
        const cancelBtn = document.getElementById('change-name-cancel');
        const saveBtn = document.getElementById('change-name-save');
        const nameInput = document.getElementById('player-name-input');

        if (cancelBtn) {
            cancelBtn.addEventListener('click', hideChangeNameDialog);
        }

        if (saveBtn) {
            saveBtn.addEventListener('click', function () {
                const newName = nameInput.value.trim();

                if (!newName || newName.length < 2 || newName.length > 12) {
                    addMessage('名称必须在2-12个字符之间！');
                    return;
                }

                if (isConnected) {
                    ws.send(JSON.stringify({
                        type: "change_name",
                        name: newName
                    }));
                    addMessage(`正在修改名称为: ${newName}`);
                } else {
                    addMessage('请先连接服务器！');
                }

                hideChangeNameDialog();
            });
        }

        // 自动聚焦到输入框
        if (nameInput) {
            setTimeout(() => {
                nameInput.focus();
                nameInput.select();
            }, 100);
        }
    }

    // 隐藏改名对话框的函数
    function hideChangeNameDialog() {
        const settingsOverlay = document.getElementById('change-name-overlay');
        if (settingsOverlay) {
            settingsOverlay.classList.add('hidden');
        }
    }

    // 修改现有的 settings-btn 点击事件
    document.getElementById('settings-btn').addEventListener('click', showChangeNameDialogCustom);

    // ===================== 设置按钮核心逻辑（修复自动模式按钮） =====================
    const settingsBtn = document.getElementById('settings-btn');
    const settingsOverlay = document.getElementById('settings-overlay');
    const settingsCancelBtn = document.getElementById('settings-cancel');
    const settingsSaveBtn = document.getElementById('settings-save');
    const controlModeToggles = document.querySelectorAll('.control-toggle');
    const manualControls = document.getElementById('manual-controls');

    // 打开设置对话框
    settingsBtn.addEventListener('click', () => {
        settingsOverlay.classList.remove('hidden');
    });

    // 关闭设置对话框（取消按钮：仅关闭，不执行任何逻辑）
    settingsCancelBtn.addEventListener('click', () => {
        settingsOverlay.classList.add('hidden');
    });

    // 点击对话框外部关闭
    settingsOverlay.addEventListener('click', (e) => {
        if (e.target === settingsOverlay) {
            settingsOverlay.classList.add('hidden');
        }
    });

    // 模式切换（自动/手动）逻辑：仅改变UI状态，不发送指令
    controlModeToggles.forEach(toggle => {
        toggle.addEventListener('change', () => {
            if (toggle.value === 'manual') {
                // 手动模式：启用手动控制选项
                manualControls.classList.remove('opacity-50', 'pointer-events-none');
            } else {
                // 自动模式：冻结手动控制选项（仅UI变化，不发送指令）
                manualControls.classList.add('opacity-50', 'pointer-events-none');
            }
        });
    });

    // 保存设置（确定按钮：执行对应模式逻辑）
    settingsSaveBtn.addEventListener('click', () => {
        const selectedMode = document.querySelector('input[name="control-mode"]:checked').value;

        if (selectedMode === 'auto') {
            // 自动模式：发送启用自动模式指令
            sendAutoModeCommand();
        } else {
            // 手动模式：发送时间+天气切换指令
            const selectedTime = document.querySelector('input[name="time"]:checked').value;
            const selectedWeather = document.querySelector('input[name="weather"]:checked').value;
            sendTimeWeatherCommand(selectedTime, selectedWeather);
        }

        // 关闭对话框
        settingsOverlay.classList.add('hidden');
    });


    // 对话框按钮事件
    document.getElementById('dialog-confirm').addEventListener('click', () => {
        hideDialog();
        // 根据当前游戏状态动态更新目标
        if (gameState.fragmentsFound >= 9) {
            // 已经收集完三块碎片，目标应该是返回NPC
            updateCurrentTargetLabel('return_to_npc');
        } else {
            // 还未收集完，目标应该是收集碎片
            updateCurrentTargetLabel('collect_fragments');
        }
        sendInteraction('dialog_confirm');
    });

    document.getElementById('dialog-cancel').addEventListener('click', () => {
        hideDialog();
        sendInteraction('dialog_cancel');
    });

    // 禁用双击缩放
    document.addEventListener('dblclick', (e) => e.preventDefault());

    // 禁用长按选择
    document.addEventListener('contextmenu', (e) => e.preventDefault());
}
// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', initApp);