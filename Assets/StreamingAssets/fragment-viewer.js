(() => {
    // 碎片ID到模型文件名的映射表
    // fragment1 -> 01.fbx, fragment2 -> 02.fbx, fragment3 -> 03.fbx, ...
    const fragmentModelMap = {
        'fragment1': './models/01/01.fbx',
        'fragment2': './models/02/02.fbx',
        'fragment3': './models/03/03.fbx',
        'fragment4': './models/04/04.fbx',
        'fragment5': './models/05/05.fbx',
        'fragment6': './models/06/06.fbx',
        'fragment7': './models/07/07.fbx',
        'fragment8': './models/08/08.fbx',
        'fragment9': './models/09/09.fbx'
    };

    // 全局变量
    let currentFragmentId = null;

    function createFragmentDialogMarkup(fragmentName) {
        return `
            <div id="fragment-3d-container" class="w-full h-96 bg-gray-900 rounded-lg relative">
                <div class="absolute top-2 left-2 text-white text-sm bg-black bg-opacity-50 px-2 py-1 rounded touch-hint">
                    单指旋转 · 双指缩放
                </div>
                <div id="fragment-loading-text" class="absolute inset-0 flex items-center justify-center text-white">
                    加载碎片模型中...
                </div>
            </div>
        `;
    }

    function showFragmentDialog(fragmentId, fragmentName) {
        if (typeof showDialog !== 'function') {
            console.error('showDialog 未定义，无法显示碎片展示窗口');
            return;
        }

        currentFragmentId = fragmentId;
        console.log(`显示碎片 ${fragmentId} 展示对话框`);

        // 解析碎片编号用于显示
        const fragmentNumber = fragmentId.replace('fragment', '');
        const dialogTitle = `文物碎片 ${fragmentNumber}：${fragmentName}`;

        showDialog(dialogTitle, createFragmentDialogMarkup(fragmentName));

        // 使用更可靠的事件设置方式
        const originalHideDialog = hideDialog;

        // 重写hideDialog函数，在关闭时发送消息
        window.hideDialog = function () {
            console.log(`关闭碎片 ${fragmentId} 展示页面`);
            sendCloseFragmentViewMessage();
            // 恢复原始函数并调用
            window.hideDialog = originalHideDialog;
            originalHideDialog();
        };

        // 初始化3D模型
        setTimeout(() => initFragment3DModel(fragmentId), 100);
    }

    function sendCloseFragmentViewMessage() {
        console.log('发送关闭碎片展示消息');

        if (typeof isConnected !== 'undefined' && isConnected) {
            if (typeof sendInteraction === 'function') {
                sendInteraction('close_fragment_view', { fragmentId: currentFragmentId });
            } else if (typeof ws !== 'undefined' && ws.readyState === WebSocket.OPEN) {
                ws.send(JSON.stringify({
                    type: 'close_fragment_view',
                    fragmentId: currentFragmentId
                }));
            }
        }
    }

    async function initFragment3DModel(fragmentId) {
        console.log(`开始初始化碎片 ${fragmentId} 的3D模型...`);

        const container = document.getElementById('fragment-3d-container');
        const loadingText = document.getElementById('fragment-loading-text');

        if (!container) {
            console.error('❌ 碎片3D容器未找到');
            return;
        }

        // 获取对应碎片的模型路径
        const modelPath = fragmentModelMap[fragmentId];
        
        console.log(`使用模型路径: ${modelPath}`);

        try {
            const { FBXLoader } = await import('https://cdn.skypack.dev/three@0.128.0/examples/jsm/loaders/FBXLoader.js');
            console.log('✅ FBXLoader动态导入成功');

            const scene = new THREE.Scene();
            scene.background = new THREE.Color(0x1a202c);

            const camera = new THREE.PerspectiveCamera(
                75,
                container.clientWidth / container.clientHeight,
                0.1,
                1000
            );
            camera.position.z = 5;

            const renderer = new THREE.WebGLRenderer({ antialias: true });
            renderer.setSize(container.clientWidth, container.clientHeight);
            container.innerHTML = '';
            container.appendChild(renderer.domElement);

            const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
            scene.add(ambientLight);
            const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
            directionalLight.position.set(5, 5, 5);
            scene.add(directionalLight);

            const fbxLoader = new FBXLoader();
            console.log(`开始加载碎片FBX模型: ${modelPath}`);

            let currentObject = null;
            let isRotating = false;
            let initialScale = 1;

            fbxLoader.load(
                modelPath,
                function (fbx) {
                    console.log(`✅ 碎片 ${fragmentId} FBX模型加载成功`);

                    scene.add(fbx);
                    currentObject = fbx;


                    // ✅ 先设置初始旋转为当前Unity中的角度
                    sendFragmentModelControlData(fragmentId, 0); // 同步初始角度

                    const bbox = new THREE.Box3().setFromObject(fbx);
                    const size = bbox.getSize(new THREE.Vector3());
                    const maxDim = Math.max(size.x, size.y, size.z);
                    const scale = 3 / maxDim;
                    fbx.scale.setScalar(scale);
                    initialScale = scale;

                    const center = bbox.getCenter(new THREE.Vector3());
                    fbx.position.sub(center.multiplyScalar(scale));

                    if (loadingText) {
                        loadingText.style.display = 'none';
                    }

                    console.log('碎片模型设置完成');
                },
                function (progress) {
                    if (loadingText && progress.lengthComputable) {
                        const percent = Math.round((progress.loaded / progress.total) * 100);
                        loadingText.textContent = `加载碎片模型中... ${percent}%`;
                    }
                },
                function (error) {
                    console.error(`❌ 碎片 ${fragmentId} FBX模型加载失败:`, error);
                    if (loadingText) {
                        loadingText.innerHTML = `
                            <div class="text-red-500 text-center">
                                <p>碎片模型加载失败: ${error.message}</p>
                                <p class="text-xs mt-2">模型路径: ${modelPath}</p>
                            </div>
                        `;
                    }
                }
            );

            const scaleState = {
                isScaling: false,
                initialDistance: 0,
                currentScale: 1,
                minScale: 0.1,
                maxScale: 8.0,
                sensitivity: 0.01
            };

            function handleTouchStart(e) {
                e.preventDefault();

                if (e.touches.length === 1) {
                    isRotating = true;
                    scaleState.isScaling = false;
                    if (currentObject) {
                        currentObject._lastTouchX = e.touches[0].clientX;
                        currentObject._lastTouchY = e.touches[0].clientY;
                    }
                } else if (e.touches.length === 2) {
                    isRotating = false;
                    scaleState.isScaling = true;
                    scaleState.initialDistance = getTouchDistance(e.touches);
                    scaleState.currentScale = currentObject ? currentObject.scale.x : 1;
                }
            }

            function handleTouchMove(e) {
                e.preventDefault();

                if (!currentObject) return;

                if (isRotating && e.touches.length === 1) {
                    handleRotation(e.touches[0]);
                } else if (scaleState.isScaling && e.touches.length === 2) {
                    handleScale(e.touches);
                }
            }

            function handleTouchEnd(e) {
                isRotating = false;
                if (e.touches.length === 0) {
                    scaleState.isScaling = false;
                    scaleState.initialDistance = 0;
                    if (currentObject) {
                        currentObject._lastTouchX = null;
                        currentObject._lastTouchY = null;
                    }
                } else if (e.touches.length === 1 && scaleState.isScaling) {
                    scaleState.isScaling = false;
                    scaleState.initialDistance = 0;
                    isRotating = true;
                    if (currentObject) {
                        currentObject._lastTouchX = e.touches[0].clientX;
                        currentObject._lastTouchY = e.touches[0].clientY;
                    }
                }
            }

            function getTouchDistance(touches) {
                const dx = touches[0].clientX - touches[1].clientX;
                const dy = touches[0].clientY - touches[1].clientY;
                return Math.sqrt(dx * dx + dy * dy);
            }

            function handleRotation(touch) {
                if (!currentObject._lastTouchX || !currentObject._lastTouchY) {
                    currentObject._lastTouchX = touch.clientX;
                    currentObject._lastTouchY = touch.clientY;
                    return;
                }

                const deltaX = touch.clientX - currentObject._lastTouchX;
                const deltaY = touch.clientY - currentObject._lastTouchY;

                const sensitivity = 0.005;

                // 只处理水平旋转（Y轴）
                if (Math.abs(deltaX) > 2) {
                    currentObject.rotation.y += deltaX * sensitivity;
                    console.log(`碎片 ${fragmentId} 旋转Y轴: ${currentObject.rotation.y.toFixed(3)} 弧度`);
                    sendFragmentModelControlData(fragmentId, currentObject.rotation.y);
                }

                currentObject._lastTouchX = touch.clientX;
                currentObject._lastTouchY = touch.clientY;
            }

            function handleScale(touches) {
                if (!scaleState.isScaling || !currentObject) return;

                const currentDistance = getTouchDistance(touches);
                const distanceDelta = currentDistance - scaleState.initialDistance;
                const scaleDelta = distanceDelta * scaleState.sensitivity;
                const targetScale = scaleState.currentScale + scaleDelta;
                const clampedScale = Math.max(scaleState.minScale, Math.min(scaleState.maxScale, targetScale));

                currentObject.scale.setScalar(clampedScale);
                sendFragmentModelControlData(fragmentId, currentObject.rotation.y);
            }

            function sendFragmentModelControlData(fragmentId, rotationY) {
                if (typeof isConnected === 'undefined' || typeof ws === 'undefined') {
                    return;
                }

                if (isConnected && currentObject) {
                    const message = {
                        type: 'fragment_model_control',
                        fragmentId: fragmentId,
                        rotation_y: rotationY
                    };
                    console.log('发送碎片模型控制数据:', message);
                    ws.send(JSON.stringify(message));
                }
            }

            renderer.domElement.addEventListener('touchstart', handleTouchStart);
            renderer.domElement.addEventListener('touchmove', handleTouchMove);
            renderer.domElement.addEventListener('touchend', handleTouchEnd);
            renderer.domElement.addEventListener('touchcancel', handleTouchEnd);

            let isMouseDragging = false;
            let previousMousePosition = { x: 0, y: 0 };

            renderer.domElement.addEventListener('mousedown', function (e) {
                isMouseDragging = true;
                container.style.cursor = 'grabbing';
                previousMousePosition = { x: e.clientX, y: e.clientY };
            });

            renderer.domElement.addEventListener('mousemove', function (e) {
                if (!isMouseDragging || !currentObject) return;

                const deltaX = e.clientX - previousMousePosition.x;
                currentObject.rotation.y += deltaX * 0.01;
                previousMousePosition = { x: e.clientX, y: e.clientY };
                sendFragmentModelControlData(fragmentId, currentObject.rotation.y);
            });

            renderer.domElement.addEventListener('mouseup', function () {
                isMouseDragging = false;
                container.style.cursor = 'grab';
            });

            function animate() {
                requestAnimationFrame(animate);
                renderer.render(scene, camera);
            }
            animate();

            window.addEventListener('resize', function () {
                camera.aspect = container.clientWidth / container.clientHeight;
                camera.updateProjectionMatrix();
                renderer.setSize(container.clientWidth, container.clientHeight);
            });
        } catch (error) {
            console.error(`❌ 碎片 ${fragmentId} 3D初始化失败:`, error);
            if (loadingText) {
                loadingText.innerHTML = `
                    <div class="text-red-500 text-center">
                        <p>碎片3D初始化失败</p>
                        <p class="text-xs">${error.message}</p>
                    </div>
                `;
            }
        }
    }

    // 暴露给全局的函数
    window.showFragmentDialog = showFragmentDialog;

    console.log('✅ fragment-viewer.js 加载完成');
})();