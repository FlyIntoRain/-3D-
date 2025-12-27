// 背包系统逻辑脚本
class Inventory {
    constructor() {
        this.items = new Map(); // 物品数据 { id: { name, icon, obtained } }
        this.inventoryOverlay = null; // 背包浮窗容器
        this.inventoryGrid = null; // 物品格子容器
        this.artifactButton = null; // 查看文物按钮
        this.isOpen = false; // 背包是否打开
        this.onItemClick = null; // 物品点击回调函数
        this.onArtifactClick = null; // 查看文物按钮回调
        
        this.init();
    }

    // 初始化背包
    init() {
        this.inventoryOverlay = document.getElementById('inventory-overlay');
        this.inventoryGrid = document.getElementById('inventory-grid');
        this.artifactButton = document.getElementById('view-artifact-btn');
        
        if (!this.inventoryOverlay || !this.inventoryGrid) {
            console.warn('背包界面元素未找到，将在显示时重试。');
            return;
        }
        
        // 绑定关闭按钮事件
        const closeBtn = document.getElementById('inventory-close-btn');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => this.close());
        }

        if (this.artifactButton) {
            this.artifactButton.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.handleArtifactClick();
            });
        }
        
        // 点击背景关闭
        this.inventoryOverlay.addEventListener('click', (e) => {
            if (e.target === this.inventoryOverlay) {
                this.close();
            }
        });
    }

    setArtifactButtonVisible(visible) {
        if (!this.artifactButton) {
            this.init();
        }
        if (!this.artifactButton) return;
        if (visible) {
            this.artifactButton.classList.remove('hidden');
        } else {
            this.artifactButton.classList.add('hidden');
        }
    }

    handleArtifactClick() {
        if (this.onArtifactClick) {
            this.onArtifactClick();
        }
        sendInteraction('Artifact_view');//新增发送查看信息
    }

    // 添加或更新物品
    addItem(itemId, itemData) {
        this.items.set(itemId, {
            name: itemData.name || '未知物品',
            icon: itemData.icon || '',
            obtained: itemData.obtained !== undefined ? itemData.obtained : false,
            description: itemData.description || ''
        });
        this.updateDisplay();
    }

    // 设置物品获得状态
    setItemObtained(itemId, obtained) {
        const item = this.items.get(itemId);
        if (item) {
            item.obtained = obtained;
            this.updateDisplay();
        }
    }

    // 批量设置物品
    setItems(itemsData) {
        if (Array.isArray(itemsData)) {
            itemsData.forEach(item => {
                if (item.id) {
                    this.addItem(item.id, item);
                }
            });
        } else if (typeof itemsData === 'object') {
            Object.keys(itemsData).forEach(itemId => {
                this.addItem(itemId, itemsData[itemId]);
            });
        }
    }

    // 获取物品信息
    getItem(itemId) {
        return this.items.get(itemId);
    }

    // 检查物品是否已获得
    isItemObtained(itemId) {
        const item = this.items.get(itemId);
        return item ? item.obtained : false;
    }

    // 更新显示
    updateDisplay() {
        if (!this.inventoryGrid) {
            // 如果元素不存在，尝试重新初始化
            this.init();
            if (!this.inventoryGrid) {
                return;
            }
        }
        
        // 清空现有内容
        this.inventoryGrid.innerHTML = '';
        
        // 创建物品格子
        this.items.forEach((item, itemId) => {
            const slot = document.createElement('div');
            slot.className = 'inventory-slot';
            slot.dataset.itemId = itemId;
            
            // 根据获得状态设置样式
            if (item.obtained) {
                slot.classList.add('obtained');
            } else {
                slot.classList.add('not-obtained');
            }
            
            // 添加图标
            if (item.icon) {
                const icon = document.createElement('img');
                icon.src = item.icon;
                icon.alt = item.name;
                icon.className = 'inventory-icon';
                slot.appendChild(icon);
            } else {
                // 如果没有图标，显示名称
                const nameLabel = document.createElement('span');
                nameLabel.textContent = item.name;
                nameLabel.className = 'inventory-name';
                slot.appendChild(nameLabel);
            }
            
            // 添加物品名称标签（可选）
            const nameTag = document.createElement('div');
            nameTag.className = 'inventory-item-name';
            nameTag.textContent = item.name;
            slot.appendChild(nameTag);
            
            // 绑定点击事件（支持PC和移动端）
            slot.addEventListener('click', (e) => {
                e.stopPropagation();
                this.handleItemClick(itemId, item);
            });
            
            // 支持触摸事件
            slot.addEventListener('touchend', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.handleItemClick(itemId, item);
            });
            
            this.inventoryGrid.appendChild(slot);
        });
    }

    // 处理物品点击
    handleItemClick(itemId, item) {
        console.log('点击物品:', itemId, item);
        
        // 触发回调
        if (this.onItemClick) {
            this.onItemClick(itemId, item);
        }


        //新增： 如果点击的是已获得的文物碎片，显示碎片模型
        if (item.obtained && itemId.startsWith('fragment')) {
            this.showFragmentModel(itemId, item);
            // ============== 新增：发送消息到Unity ==============
            this.sendFragmentMessageToUnity(itemId, item);
        }
    }

    // ============== 新增：发送碎片消息到Unity的方法 ==============
    sendFragmentMessageToUnity(fragmentId, fragmentData) {
        console.log(`发送查看碎片消息到Unity: ${fragmentId}`);

        // 使用你已有的sendInteraction函数
        if (typeof sendInteraction === 'function') {
            sendInteraction('view_fragment', {
                fragmentId: fragmentId,
                fragmentName: fragmentData.name,
                timestamp: new Date().toISOString(),
                data: {
                    name: fragmentData.name,
                    description: fragmentData.description || '',
                    obtained: fragmentData.obtained
                }
            });
            console.log('✅ 通过sendInteraction发送消息到Unity');
        } else if (typeof ws !== 'undefined' && ws.readyState === WebSocket.OPEN) {
            // 备用方案：直接发送WebSocket消息
            const message = {
                type: 'view_fragment',
                fragmentId: fragmentId,
                fragmentName: fragmentData.name,
                timestamp: new Date().toISOString()
            };
            ws.send(JSON.stringify(message));
            console.log('✅ 直接通过WebSocket发送消息到Unity:', message);
        } else {
            console.warn('❌ 无法发送消息到Unity');
        }
    }
    // ============== 新增方法结束 ==============
    // 新增：显示碎片模型
    showFragmentModel(fragmentId, fragmentData) {
        console.log(`显示碎片模型: ${fragmentId}, ${fragmentData.name}`);

        // 检查是否加载了fragment-viewer.js
        if (typeof window.showFragmentDialog === 'function') {
            window.showFragmentDialog(fragmentId, fragmentData.name);
        } else {
            console.error('fragment-viewer.js 未加载，无法显示碎片模型');

            // 回退方案：显示简单的对话框
            if (typeof showDialog === 'function') {
                // 解析碎片编号
                const fragmentNumber = fragmentId.replace('fragment', '');
                showDialog(
                    `文物碎片 ${fragmentNumber}`,
                    `<p>${fragmentData.name}</p>
                 <p>描述: ${fragmentData.description || '暂无描述'}</p>`
                );
            }
        }
    }


    // 打开背包
    open() {
        if (!this.inventoryOverlay) {
            this.init();
            if (!this.inventoryOverlay) {
                console.error('背包界面元素未找到，无法打开');
                return;
            }
        }
        
        this.updateDisplay();
        this.inventoryOverlay.classList.remove('hidden');
        this.isOpen = true;
    }

    // 关闭背包
    close() {
        if (this.inventoryOverlay) {
            this.inventoryOverlay.classList.add('hidden');
            this.isOpen = false;
        }
    }

    // 切换背包显示状态
    toggle() {
        if (this.isOpen) {
            this.close();
        } else {
            this.open();
        }
    }

    // 重置背包（清空所有物品）
    reset() {
        this.items.clear();
        this.updateDisplay();
    }
}

// 导出供其他脚本使用
if (typeof window !== 'undefined') {
    window.Inventory = Inventory;
}





