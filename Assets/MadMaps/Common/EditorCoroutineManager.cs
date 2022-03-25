using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MadMaps.Common
{
    public static class EditorCoroutineManager
    {
        class CoroutineState
        {
            public int Priority;
            public IEnumerator Coroutine;
            public Action OnCouroutineCompleted;

            public CoroutineState(IEnumerator couroutine, Action onCouroutineCompleted, int priority)
            {
                Coroutine = couroutine;
                OnCouroutineCompleted = onCouroutineCompleted;
                Priority = priority;
            }
        }

        private const int Budget = 10;

        static List<CoroutineState>  _runningCoroutines = new List<CoroutineState>();

        //private static Dictionary<IEnumerator, Action> _runningCoroutines = new Dictionary<IEnumerator, Action>();
        private static Dictionary<Action, DateTime> _invokes = new Dictionary<Action, DateTime>();
        public static int CoroutineCount { get { return _runningCoroutines.Count; } }

        public static void UpdateCoroutines()
        {
            for (var i = 0; i < Budget; ++i)
            {
                if (_runningCoroutines.Count == 0)
                {
                    break;
                }
                var pair = _runningCoroutines.First();
                var coroutine = pair.Coroutine;
                if (!coroutine.MoveNext())
                {
                    try
                    {
                        if (pair.OnCouroutineCompleted != null)
                        {
                            pair.OnCouroutineCompleted.Invoke();
                        }
                    }
                    finally
                    {
                        _runningCoroutines.Remove(pair);
                    }
                }
            }

            if (_invokes.Count > 0)
            {
                var invokeEnumerator = _invokes.GetEnumerator();
                var now = DateTime.UtcNow;

                List<Action> toExecute = new List<Action>();

                while (invokeEnumerator.MoveNext())
                {
                    var timeToExecute = invokeEnumerator.Current.Value;
                    if (timeToExecute < now)
                    {
                        toExecute.Add(invokeEnumerator.Current.Key);
                    }
                }

                for (int i = 0; i < toExecute.Count; i++)
                {
                    var action = toExecute[i];
                    _invokes.Remove(action);
                    action.Invoke();
                }
            }
        }

        public static IEnumerator StartEditorCoroutine(IEnumerator couroutine, Action callback = null, int priority = 0)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= UpdateCoroutines;
            UnityEditor.EditorApplication.update += UpdateCoroutines;
#endif
            _runningCoroutines.Add(new CoroutineState(couroutine, callback, priority));
            _runningCoroutines.Sort((state, coroutineState) => state.Priority.CompareTo(coroutineState.Priority));
            return couroutine;
        }

        public static void Invoke(Action callback, int seconds)
        {
            _invokes.Add(callback, DateTime.UtcNow.ToUniversalTime() + new TimeSpan(0, 0, 0, seconds));
        }

        public static void StopEditorCoroutine(IEnumerator coroutine)
        {
            _runningCoroutines.RemoveAll(state => state.Coroutine == coroutine);
        }
    }
}