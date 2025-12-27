(() => {
    // 添加调试函数
    function debugSendTestMessage() {
        if (typeof isConnected !== 'undefined' && isConnected && typeof ws !== 'undefined' && ws.readyState === WebSocket.OPEN) {
            // 测试旋转消息
            const rotationMsg = { type: 'model_control', rotation_y: 1.57 };
            ws.send(JSON.stringify(rotationMsg));
            console.log('测试发送旋转消息:', rotationMsg);

            // 测试关闭消息
            const closeMsg = { type: 'close_artifact_view' };
            ws.send(JSON.stringify(closeMsg));
            console.log('测试发送关闭消息:', closeMsg);
        }
    }

    // 暴露调试函数
    window.debugSendTestMessage = debugSendTestMessage;

    function createDialogMarkup() {
        return `
            <div id="3d-container" class="w-full h-96 bg-gray-900 rounded-lg relative">
                <div class="absolute top-2 left-2 text-white text-sm bg-black bg-opacity-50 px-2 py-1 rounded touch-hint">
                    单指旋转 · 双指缩放
                </div>
                <div id="loading-text" class="absolute inset-0 flex items-center justify-center text-white">
                    加载3D模型中...
                </div>
            </div>
        `;
    }

    function showArtifactDialog() {
        if (typeof showDialog !== 'function') {
            console.error('showDialog 未定义，无法显示文物展示窗口');
            return;
        }

        console.log('显示文物展示对话框');
        showDialog('3D文物模型', createDialogMarkup());

        // 使用更可靠的事件设置方式
        const originalHideDialog = hideDialog;

        // 重写hideDialog函数，在关闭时发送消息
        window.hideDialog = function () {
            console.log('关闭文物展示页面');
            sendCloseArtifactViewMessage();
            // 恢复原始函数并调用
            window.hideDialog = originalHideDialog;
            originalHideDialog();
        };

        // 初始化3D模型
        setTimeout(init3DModel, 100);
    }

    function sendCloseArtifactViewMessage() {
        console.log('发送关闭文物展示消息');

        if (typeof isConnected !== 'undefined' && isConnected) {
            if (typeof sendInteraction === 'function') {
                sendInteraction('close_artifact_view');
            } else if (typeof ws !== 'undefined' && ws.readyState === WebSocket.OPEN) {
                ws.send(JSON.stringify({ type: 'close_artifact_view' }));
            }
        }
    }

    async function init3DModel() {
        console.log('开始初始化3D模型...');

        const container = document.getElementById('3d-container');
        const loadingText = document.getElementById('loading-text');

        if (!container) {
            console.error('❌ 3D容器未找到');
            return;
        }

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
            const modelPath = './models/鎏金炉2.fbx';

            console.log('开始加载FBX模型...');

            let currentObject = null;
            let isRotating = false;
            let initialScale = 1; // 声明变量

            fbxLoader.load(
                modelPath,
                function (fbx) {
                    console.log('✅ FBX模型加载成功');

                    scene.add(fbx);
                    currentObject = fbx;

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

                    console.log('模型设置完成');
                },
                function (progress) {
                    if (loadingText && progress.lengthComputable) {
                        const percent = Math.round((progress.loaded / progress.total) * 100);
                        loadingText.textContent = `加载3D模型中... ${percent}%`;
                    }
                },
                function (error) {
                    console.error('❌ FBX模型加载失败:', error);
                    if (loadingText) {
                        loadingText.innerHTML = `
                            <div class="text-red-500 text-center">
                                <p>模型加载失败: ${error.message}</p>
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
                    console.log(`旋转Y轴: ${currentObject.rotation.y.toFixed(3)} 弧度`);
                    sendModelControlData();
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
                sendModelControlData();
            }

            // 只有一个 sendModelControlData 函数
            function sendModelControlData() {
                if (typeof isConnected === 'undefined' || typeof ws === 'undefined') {
                    return;
                }

                if (isConnected && currentObject) {
                    // 简化消息格式
                    const message = {
                        type: 'model_control',
                        rotation_y: currentObject.rotation.y
                    };
                    console.log('发送模型控制数据:', message);
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
                sendModelControlData();
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
            console.error('❌ 3D初始化失败:', error);
            if (loadingText) {
                loadingText.innerHTML = `
                    <div class="text-red-500 text-center">
                        <p>3D初始化失败</p>
                        <p class="text-xs">${error.message}</p>
                    </div>
                `;
            }
        }
    }

    window.showArtifactDialog = showArtifactDialog;
})();