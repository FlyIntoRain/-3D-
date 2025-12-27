using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    // 单例实例
    private static MainThreadDispatcher _instance;
    public static MainThreadDispatcher Instance => _instance;

    // 存储需要在主线程执行的任务
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 每帧在主线程中执行队列中的任务
        lock (_mainThreadActions)
        {
            while (_mainThreadActions.Count > 0)
            {
                _mainThreadActions.Dequeue().Invoke();
            }
        }
    }

    // 向主线程队列添加任务
    public void Enqueue(Action action)
    {
        lock (_mainThreadActions)
        {
            _mainThreadActions.Enqueue(action);
        }
    }
}