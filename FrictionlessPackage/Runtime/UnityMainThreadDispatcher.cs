using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Frictionless
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static int _mainThreadId;
        private static readonly ConcurrentQueue<Action> _pendingActions = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Application.isPlaying)
            {
                _mainThreadId = Thread.CurrentThread.ManagedThreadId;
                var go = new GameObject("UnityMainThreadDispatcher").AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }

        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        public static void DoOnMainThread(Action action)
        {
            _pendingActions.Enqueue(action);
        }

        private void Update()
        {
            while (_pendingActions.TryDequeue(out var pendingAction))
            {
                pendingAction.Invoke();
            }
        }
    }
}