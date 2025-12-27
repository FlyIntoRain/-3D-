// 拼图游戏逻辑脚本
class PuzzleGame {
    constructor() {
        this.gridSize = 3; // 3x3网格
        this.pieces = []; // 拼图块数组
        this.puzzleArea = null; // 拼图区域容器
        this.piecesContainer = null; // 拼图块容器
        this.referenceImage = null; // 参考图片元素
        this.isDragging = false; // 是否正在拖拽
        this.draggedPiece = null; // 当前拖拽的拼图块
        this.dragOffset = { x: 0, y: 0 }; // 拖拽偏移量
        this.placedPieces = new Map(); // 已放置的拼图块 { position: piece }
        this.imageUrl = null; // 图片URL
        this.cellSize = 100; // 每个格子的尺寸（像素）
        this.initialized = false; // 是否已初始化
        
        this.init();
    }

    // 初始化游戏
    init() {
        this.puzzleArea = document.getElementById('puzzle-area');
        this.piecesContainer = document.getElementById('puzzle-pieces-container');
        this.referenceImage = document.getElementById('reference-image');
        
        if (!this.puzzleArea || !this.piecesContainer || !this.referenceImage) {
            console.warn('拼图游戏元素未找到，可能在界面隐藏时初始化。将在startGame时重试。');
            this.initialized = false;
            return;
        }
        
        // 设置拼图区域尺寸
        const areaSize = this.gridSize * this.cellSize;
        this.puzzleArea.style.width = areaSize + 'px';
        this.puzzleArea.style.height = areaSize + 'px';
        
        // 创建拼图区域网格
        this.createGrid();
        
        // 绑定事件
        this.bindEvents();
        
        this.initialized = true;
    }

    // 加载图片并开始游戏
    async startGame(imageUrl) {
        // 如果未初始化，尝试重新初始化
        if (!this.initialized) {
            this.init();
            if (!this.initialized) {
                console.error('拼图游戏元素仍未找到，无法开始游戏');
                return;
            }
        }
        
        // 默认使用本地相对路径图片，如果传入则使用传入的路径
        this.imageUrl = imageUrl || 'relic.png';
        
        // 处理相对路径：如果不是完整URL，则使用相对路径
        if (!this.isAbsoluteUrl(this.imageUrl)) {
            // 确保相对路径正确（相对于当前HTML文件）
            // 如果图片在同一目录下，直接使用；否则根据需要调整路径
            // 例如：'./test.png' 或 'test.png' 都可以
        }
        
        try {
            // 预加载图片
            await this.preloadImage(this.imageUrl);
            
            // 清空之前的状态
            this.placedPieces.clear();
            this.puzzleArea.innerHTML = '';
            this.createGrid();
            
            // 创建拼图块
            this.createPuzzlePieces();
            
            // 显示参考图片
            if (this.referenceImage) {
                this.referenceImage.src = this.imageUrl;
                this.referenceImage.style.display = 'block';
            }
            
            // 打乱拼图块
            this.shufflePieces();
            
        } catch (error) {
            console.error('加载图片失败:', error);
            alert(`加载图片失败: ${this.imageUrl}\n请检查图片路径是否正确，确保图片文件存在于指定位置。`);
        }
    }

    // 判断是否为绝对URL
    isAbsoluteUrl(url) {
        return /^https?:\/\//.test(url) || /^\/\//.test(url) || /^data:/.test(url);
    }

    // 预加载图片
    preloadImage(url) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            
            // 如果是本地相对路径，不需要设置crossOrigin
            // 只有跨域图片才需要设置crossOrigin
            if (this.isAbsoluteUrl(url) && !url.startsWith('data:')) {
                img.crossOrigin = 'anonymous';
            }
            
            img.onload = () => {
                console.log('图片加载成功:', url);
                resolve(img);
            };
            img.onerror = (error) => {
                console.error('图片加载失败:', url, error);
                reject(new Error(`无法加载图片: ${url}`));
            };
            img.src = url;
        });
    }

    // 创建拼图区域网格
    createGrid() {
        this.puzzleArea.innerHTML = '';
        
        for (let row = 0; row < this.gridSize; row++) {
            for (let col = 0; col < this.gridSize; col++) {
                const cell = document.createElement('div');
                cell.className = 'puzzle-cell';
                cell.dataset.row = row;
                cell.dataset.col = col;
                cell.style.width = this.cellSize + 'px';
                cell.style.height = this.cellSize + 'px';
                this.puzzleArea.appendChild(cell);
            }
        }
    }

    // 创建拼图块
    createPuzzlePieces() {
        // 清空之前的拼图块
        this.pieces = [];
        if (this.piecesContainer) {
            this.piecesContainer.innerHTML = '';
        }
        
        const pieceSize = 300 / this.gridSize; // 原图每块的大小
        
        for (let row = 0; row < this.gridSize; row++) {
            for (let col = 0; col < this.gridSize; col++) {
                const piece = document.createElement('div');
                piece.className = 'puzzle-piece';
                piece.dataset.position = row * this.gridSize + col;
                piece.dataset.row = row;
                piece.dataset.col = col;
                
                // 设置背景图片位置，显示对应的图片片段
                const bgX = -col * pieceSize;
                const bgY = -row * pieceSize;
                piece.style.backgroundImage = `url(${this.imageUrl})`;
                piece.style.backgroundSize = '300px 300px';
                piece.style.backgroundPosition = `${bgX}px ${bgY}px`;
                piece.style.width = this.cellSize + 'px';
                piece.style.height = this.cellSize + 'px';
                
                if (this.piecesContainer) {
                    this.piecesContainer.appendChild(piece);
                    this.pieces.push(piece);
                }
            }
        }
    }

    // 打乱拼图块顺序
    shufflePieces() {
        const container = this.piecesContainer;
        const pieces = Array.from(container.children);
        
        // Fisher-Yates 洗牌算法
        for (let i = pieces.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [pieces[i], pieces[j]] = [pieces[j], pieces[i]];
        }
        
        // 重新添加到容器（打乱顺序）
        pieces.forEach(piece => container.appendChild(piece));
    }

    // 绑定事件
    bindEvents() {
        // 为拼图块容器绑定拖拽事件（事件委托）
        if (this.piecesContainer) {
            this.piecesContainer.addEventListener('mousedown', this.handleMouseDown.bind(this));
            this.piecesContainer.addEventListener('touchstart', this.handleTouchStart.bind(this), { passive: false });
        }
        
        // 为拼图区域绑定事件（允许拖拽已放置的拼图块）
        if (this.puzzleArea) {
            this.puzzleArea.addEventListener('mousedown', this.handleMouseDown.bind(this));
            this.puzzleArea.addEventListener('touchstart', this.handleTouchStart.bind(this), { passive: false });
        }
        
        // 全局事件监听
        document.addEventListener('mousemove', this.handleMouseMove.bind(this));
        document.addEventListener('mouseup', this.handleMouseUp.bind(this));
        
        document.addEventListener('touchmove', this.handleTouchMove.bind(this), { passive: false });
        document.addEventListener('touchend', this.handleTouchEnd.bind(this));
    }

    // 鼠标按下事件
    handleMouseDown(e) {
        const piece = e.target.closest('.puzzle-piece');
        if (!piece) return;
        
        e.preventDefault();
        this.startDrag(piece, e.clientX, e.clientY);
    }

    // 触摸开始事件
    handleTouchStart(e) {
        const piece = e.target.closest('.puzzle-piece');
        if (!piece) return;
        
        e.preventDefault();
        const touch = e.touches[0];
        this.startDrag(piece, touch.clientX, touch.clientY);
    }

    // 开始拖拽
    startDrag(piece, clientX, clientY) {
        this.isDragging = true;
        this.draggedPiece = piece;
        
        // 如果拼图块已经放置，需要先从放置位置移除
        if (piece.dataset.placed === 'true') {
            this.removePieceFromPlacement(piece);
        }
        
        const rect = piece.getBoundingClientRect();
        this.dragOffset.x = clientX - rect.left - rect.width / 2;
        this.dragOffset.y = clientY - rect.top - rect.height / 2;
        
        piece.style.position = 'fixed';
        piece.style.zIndex = '1000';
        piece.style.pointerEvents = 'none';
        
        this.updatePiecePosition(piece, clientX, clientY);
    }

    // 从放置位置移除拼图块
    removePieceFromPlacement(piece) {
        // 找到该拼图块所在的位置并移除
        for (const [key, placedPiece] of this.placedPieces.entries()) {
            if (placedPiece === piece) {
                this.placedPieces.delete(key);
                break;
            }
        }
        
        // 从拼图区域移除，但不删除拼图块本身
        // 重置状态（但保持在拼图区域中，稍后会被拖拽）
    }

    // 鼠标移动事件
    handleMouseMove(e) {
        if (!this.isDragging) return;
        e.preventDefault();
        this.updatePiecePosition(this.draggedPiece, e.clientX, e.clientY);
        this.highlightDropZone(e.clientX, e.clientY);
    }

    // 触摸移动事件
    handleTouchMove(e) {
        if (!this.isDragging) return;
        e.preventDefault();
        const touch = e.touches[0];
        this.updatePiecePosition(this.draggedPiece, touch.clientX, touch.clientY);
        this.highlightDropZone(touch.clientX, touch.clientY);
    }

    // 更新拼图块位置
    updatePiecePosition(piece, clientX, clientY) {
        piece.style.left = (clientX - this.dragOffset.x) + 'px';
        piece.style.top = (clientY - this.dragOffset.y) + 'px';
    }

    // 高亮可放置区域
    highlightDropZone(clientX, clientY) {
        // 清除之前的高亮
        document.querySelectorAll('.puzzle-cell').forEach(cell => {
            cell.classList.remove('highlight');
        });
        
        // 找到鼠标下的格子
        const cell = document.elementFromPoint(clientX, clientY)?.closest('.puzzle-cell');
        if (cell) {
            // 检查该位置是否已有拼图块（排除当前拖拽的拼图块）
            const positionKey = `${cell.dataset.row}-${cell.dataset.col}`;
            const existingPiece = this.placedPieces.get(positionKey);
            
            // 如果位置为空，或者是当前拖拽的拼图块所在的位置，则高亮
            if (!existingPiece || existingPiece === this.draggedPiece) {
                cell.classList.add('highlight');
            }
        }
    }

    // 鼠标释放事件
    handleMouseUp(e) {
        if (!this.isDragging) return;
        
        const cell = document.elementFromPoint(e.clientX, e.clientY)?.closest('.puzzle-cell');
        this.endDrag(cell);
    }

    // 触摸结束事件
    handleTouchEnd(e) {
        if (!this.isDragging) return;
        
        const touch = e.changedTouches[0];
        const cell = document.elementFromPoint(touch.clientX, touch.clientY)?.closest('.puzzle-cell');
        this.endDrag(cell);
    }

    // 结束拖拽
    endDrag(cell) {
        if (!this.draggedPiece) return;
        
        // 清除高亮
        document.querySelectorAll('.puzzle-cell').forEach(c => {
            c.classList.remove('highlight');
        });
        
        // 如果拖拽到拼图区域的格子上，则放置（无论位置是否正确）
        if (cell) {
            // 检查该位置是否已有其他拼图块
            const positionKey = `${cell.dataset.row}-${cell.dataset.col}`;
            const existingPiece = this.placedPieces.get(positionKey);
            
            // 如果位置已被其他拼图块占用，交换位置
            if (existingPiece && existingPiece !== this.draggedPiece) {
                this.swapPieces(this.draggedPiece, existingPiece, cell);
            } else {
                // 放置拼图块
                this.placePiece(this.draggedPiece, cell);
            }
            
            // 检查是否完成
            if (this.checkCompletion()) {
                this.onGameComplete();
            }
        } else {
            // 如果没有放到格子上，返回原位置
            this.returnPiece(this.draggedPiece);
        }
        
        // 确保清除拖拽时的固定定位样式
        if (this.draggedPiece) {
            // 如果拼图块不在任何容器中，需要先放回容器
            if (!this.piecesContainer.contains(this.draggedPiece) && 
                !this.puzzleArea.contains(this.draggedPiece)) {
                if (this.piecesContainer) {
                    this.piecesContainer.appendChild(this.draggedPiece);
                }
            }
        }
        
        // 重置拖拽状态
        this.isDragging = false;
        this.draggedPiece = null;
    }

    // 交换两个拼图块的位置
    swapPieces(piece1, piece2, targetCell) {
        // 获取piece2当前的位置
        const piece2Position = this.findPiecePosition(piece2);
        
        // 先移除piece2
        if (piece2Position) {
            this.placedPieces.delete(piece2Position);
        }
        
        // 放置piece1到目标位置
        this.placePiece(piece1, targetCell);
        
        // 如果piece2有原位置，将piece2放回原位置
        if (piece2Position) {
            const [row, col] = piece2Position.split('-').map(Number);
            const originalCell = this.puzzleArea.querySelector(`[data-row="${row}"][data-col="${col}"]`);
            if (originalCell) {
                this.placePiece(piece2, originalCell);
            } else {
                // 如果找不到原位置，返回拼图块容器
                this.returnPiece(piece2);
            }
        } else {
            this.returnPiece(piece2);
        }
    }

    // 查找拼图块的位置
    findPiecePosition(piece) {
        for (const [key, placedPiece] of this.placedPieces.entries()) {
            if (placedPiece === piece) {
                return key;
            }
        }
        return null;
    }

    // 检查是否可以放置拼图块
    canPlacePiece(piece, cell) {
        const targetRow = parseInt(cell.dataset.row);
        const targetCol = parseInt(cell.dataset.col);
        const pieceRow = parseInt(piece.dataset.row);
        const pieceCol = parseInt(piece.dataset.col);
        
        return targetRow === pieceRow && targetCol === pieceCol;
    }

    // 放置拼图块
    placePiece(piece, cell) {
        const row = parseInt(cell.dataset.row);
        const col = parseInt(cell.dataset.col);
        const positionKey = `${row}-${col}`;
        
        // 如果该位置已有拼图块，先移除
        if (this.placedPieces.has(positionKey)) {
            const existingPiece = this.placedPieces.get(positionKey);
            if (existingPiece !== piece) {
                // 移除旧拼图块，但不删除元素
                existingPiece.remove();
                if (this.piecesContainer) {
                    this.piecesContainer.appendChild(existingPiece);
                }
                existingPiece.dataset.placed = 'false';
                existingPiece.style.position = '';
                existingPiece.style.left = '';
                existingPiece.style.top = '';
                existingPiece.style.zIndex = '';
                existingPiece.style.pointerEvents = '';
            }
        }
        
        // 确保拼图块在拼图区域内
        if (!this.puzzleArea.contains(piece)) {
            piece.remove();
            this.puzzleArea.appendChild(piece);
        }
        
        // 使用相对定位，基于格子的位置
        // 计算格子相对于拼图区域的位置（使用布局位置而非屏幕位置）
        // 考虑边框：格子有1px边框，所以位置需要加上边框宽度
        const cellLeft = col * this.cellSize;
        const cellTop = row * this.cellSize;
        
        // 标记为已放置
        piece.dataset.placed = 'true';
        piece.style.position = 'absolute';
        piece.style.left = cellLeft + 'px';
        piece.style.top = cellTop + 'px';
        // 放置时拼图块应该与格子完全匹配（减去边框）
        piece.style.width = this.cellSize + 'px';
        piece.style.height = this.cellSize + 'px';
        piece.style.zIndex = '100';
        piece.style.pointerEvents = 'auto'; // 允许继续拖拽
        piece.style.cursor = 'move';
        piece.style.borderRadius = '0'; // 放置时移除圆角以完美对齐
        
        // 记录位置
        this.placedPieces.set(positionKey, piece);
    }

    // 返回拼图块到原位置
    returnPiece(piece) {
        // 先确保拼图块在正确的容器中
        // 如果拼图块不在容器中，先移回容器
        if (!this.piecesContainer.contains(piece) && !this.puzzleArea.contains(piece)) {
            // 拼图块可能还在文档中但没有父元素
            piece.remove();
            if (this.piecesContainer) {
                this.piecesContainer.appendChild(piece);
            }
        } else if (this.puzzleArea.contains(piece)) {
            // 如果是从拼图区域返回，需要移除并放回拼图块容器
            piece.remove();
            if (this.piecesContainer) {
                this.piecesContainer.appendChild(piece);
            }
        }
        
        // 重置所有样式
        piece.dataset.placed = 'false';
        
        // 先清除固定定位，这是关键
        piece.style.position = '';
        
        // 清除所有定位样式
        piece.style.left = '';
        piece.style.top = '';
        piece.style.right = '';
        piece.style.bottom = '';
        
        // 显式设置尺寸（重要：确保拼图块有正确的尺寸）
        piece.style.width = this.cellSize + 'px';
        piece.style.height = this.cellSize + 'px';
        
        // 设置flex属性防止压缩
        piece.style.flexShrink = '0';
        piece.style.flexGrow = '0';
        piece.style.flexBasis = 'auto';
        
        // 清除其他样式
        piece.style.zIndex = '';
        piece.style.pointerEvents = '';
        piece.style.cursor = '';
        piece.style.borderRadius = ''; // 恢复圆角
        piece.style.border = ''; // 恢复原始边框
        piece.style.transform = ''; // 清除可能的transform
        piece.style.margin = ''; // 清除margin
        piece.style.maxWidth = ''; // 清除最大宽度
        piece.style.maxHeight = ''; // 清除最大高度
        piece.style.minWidth = ''; // 清除最小宽度
        piece.style.minHeight = ''; // 清除最小高度
    }

    // 检查游戏是否完成
    checkCompletion() {
        // 首先检查所有拼图块是否都已放置
        if (this.placedPieces.size !== this.gridSize * this.gridSize) {
            return false;
        }
        
        // 检查每个拼图块是否在正确的位置
        for (const [positionKey, piece] of this.placedPieces.entries()) {
            const [cellRow, cellCol] = positionKey.split('-').map(Number);
            const pieceRow = parseInt(piece.dataset.row);
            const pieceCol = parseInt(piece.dataset.col);
            
            // 如果拼图块的位置与格子位置不匹配，则未完成
            if (pieceRow !== cellRow || pieceCol !== cellCol) {
                return false;
            }
        }
        
        // 所有拼图块都在正确位置
        return true;
    }

    // 游戏完成回调
    onGameComplete() {
        // 显示完成提示
        setTimeout(() => {
            alert('恭喜！拼图完成！');
            // 可以触发回调或发送消息
            if (this.onComplete) {
                this.onComplete();
            }
        }, 100);
    }

    // 重置游戏
    reset() {
        // 清空已放置的拼图块
        this.placedPieces.clear();
        
        // 移除所有拼图块（包括已放置的）
        const allPieces = this.puzzleArea.querySelectorAll('.puzzle-piece');
        allPieces.forEach(piece => piece.remove());
        this.piecesContainer.innerHTML = '';
        this.pieces = [];
        
        // 重新创建并打乱
        if (this.imageUrl) {
            this.createPuzzlePieces();
            this.shufflePieces();
        }
        
        // 清空拼图区域并重新创建网格
        this.puzzleArea.innerHTML = '';
        this.createGrid();
    }
}

// 导出供其他脚本使用
if (typeof window !== 'undefined') {
    window.PuzzleGame = PuzzleGame;
}

