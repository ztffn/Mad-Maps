using UnityEngine;

namespace MadMaps.Common
{
    public static class ObjectExtensions
    {
        public static T JSONClone<T>(this T o)
        {
            return JsonUtility.FromJson<T>(JsonUtility.ToJson(o));
        }

        public static bool TryDestroyImmediate(this UnityEngine.Object obj)
        {
            if (!obj)
            {
                return false;
            }
            UnityEngine.Object.DestroyImmediate(obj, true);
            return true;
        }
    }
}