using UnityEngine;

namespace MadMaps.Common
{
    public class LevelSingleton<T> : MonoBehaviour where T : Object
    {
        private static double lastCheck;
        private static object _levelInstance;

        protected LevelSingleton()
        {
            _levelInstance = this;
        }

        public static T LevelInstance
        {
            get
            {
#if UNITY_EDITOR
                var t = UnityEditor.EditorApplication.timeSinceStartup;
#else
                var t = Time.time;
#endif
                if (t < lastCheck + 5)
                {
                    return _levelInstance as T;
                }

                lastCheck = t;
                if (_levelInstance == null)
                {
                    _levelInstance = FindObjectOfType<T>();
                }
                return _levelInstance as T;
            }
        }
    }
}