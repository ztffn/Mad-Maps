using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MadMaps.Common
{
    public static class GameObjectExtensions
    {
        public static void OptimizeAndFlattenHierarchy(Transform root)
        {
            FlattenHierarchyRecursive(root, root);
            DestroyEmptyObjectsInHierarchy(root);
        }

        public static void DestroyEmptyObjectsInHierarchy(Transform root)
        {
            var childCache = new List<Transform>();
            foreach (Transform child in root)
            {
                childCache.Add(child);
            }
            for (int i = 0; i < childCache.Count; i++)
            {
                DestroyEmptyObjectsInHierarchy(childCache[i]);
                if (childCache[i].childCount == 0 && childCache[i].GetComponents<Component>().Length == 1)
                {
                    Object.DestroyImmediate(childCache[i].gameObject);
                }
            }
        }
        
        private static void FlattenHierarchyRecursive(Transform transform, Transform root)
        {
            var childCache = new List<Transform>();
            foreach (Transform child in transform)
            {
                childCache.Add(child);
            }
            for (int i = 0; i < childCache.Count; i++)
            {
                childCache[i].SetParent(root);
                FlattenHierarchyRecursive(childCache[i], root);
            }
        }
        
        public static List<T> GetComponentsInChildrenNonAlloc<T>(this GameObject go, List<T> ret) where T : Component
        {
            var thisT = go.GetComponent<T>();
            if (thisT != null)
            {
                ret.Add(thisT);
            }
            foreach (Transform child in go.transform)
            {
                child.gameObject.GetComponentsInChildrenNonAlloc(ret);
            }
            return ret;
        }

        public delegate bool ComponentTestDelegate(GameObject gameObject);

        private static HashSet<Component> _recursiveRemovalCache = new HashSet<Component>();

        public static void RemoveComponentRecursive<T>(this GameObject gameObject) where T : Component
        {
            _recursiveRemovalCache.Clear();
            while (true)
            {
                var component = gameObject.GetComponent<T>();
                if (component && !_recursiveRemovalCache.Contains(component))
                {
                    _recursiveRemovalCache.Add(component);
                    UnityEngine.Object.Destroy(component);
                    //Debug.Log("Removed component " + component, component);
                }
                else
                {
                    break;
                }
            }
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.RemoveComponentRecursive<T>();
            }
        }

        public static string GetIsNullString(this object obj)
        {
            return obj == null ? "null" : "not null";
        }

        public static void AddComponentRecursive<T>(this GameObject gameObject, ComponentTestDelegate testDelegate) where T : Component
        {
            gameObject.AddComponent<T>(testDelegate);
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.AddComponentRecursive<T>(testDelegate);
            }
        }

        public static T AddComponent<T>(this GameObject gameObject, ComponentTestDelegate testDelegate) where T : Component
        {
            if (testDelegate(gameObject))
            {
                return gameObject.AddComponent<T>();
            }
            return null;
        }

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T ret = gameObject.GetComponent<T>();
            if (ret == null)
            {
                ret = gameObject.AddComponent<T>();
            }
            return ret;
        }

        public static Component GetOrAddComponent(this GameObject gameObject, Type t)
        {
            Component ret = gameObject.GetComponent(t);
            if (ret == null)
            {
                ret = gameObject.AddComponent(t);
            }
            return ret;
        }
        
        public static void TryDestroyComponent<T>(this GameObject go, bool immediate = false) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp != null)
            {
                if (immediate)
                {
                    Object.DestroyImmediate(comp, true);
                }
                else
                {
                    Object.Destroy(comp);
                }
            }
        }

        public static Transform FindTransformInChildren(this Transform trans, string name)
        {
            if (trans.name == name)
            {
                return trans;
            }

            for (var i = 0; i < trans.childCount; i++)
            {
                var child = trans.GetChild(i);
                var result = FindTransformInChildren(child, name);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static bool IsValid(this Quaternion quaternion)
        {
            //var val = quaternion.x + quaternion.y + quaternion.z + quaternion.w;
            bool isNaN = Single.IsNaN(quaternion.x) || Single.IsNaN(quaternion.y) || Single.IsNaN(quaternion.z) || Single.IsNaN(quaternion.w) || Single.IsInfinity(quaternion.x) || Single.IsInfinity(quaternion.y) || Single.IsInfinity(quaternion.z) || Single.IsInfinity(quaternion.w);

            //bool isZero = quaternion.x == 0 && quaternion.y == 0 && quaternion.z == 0 && quaternion.w == 0;

            return !(isNaN);
        }

        public static int ComputeHash(this byte[] data)
        {
            unchecked
            {
                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < data.Length; i++)
                    hash = (hash ^ data[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }

        public static bool IsNan(this Vector3 self)
        {
            return Single.IsNaN(self.x) || Single.IsNaN(self.y) || Single.IsNaN(self.z) || Single.IsInfinity(self.x + self.y + self.z);
        }

        public static Vector2 FixNan(this Vector2 self)
        {
            if (Single.IsInfinity(self.x + self.y))
            {
                self = Vector2.zero;
            }

            return self;
        }

        public static Vector3 FixNan(this Vector3 self)
        {
            if (Single.IsInfinity(self.x + self.y + self.z))
            {
                self = Vector3.zero;
            }

            return self;
        }



        public static T GetComponentInChildrenDisabled<T>(this Transform transform) where T : Component
        {
            var ret = new List<T>();
            Traverse(transform, ret);
            return ret.Count > 0 ? ret[0] : null;
        }

        public static T[] GetComponentsInSelfAndChildrenDisabled<T>(this Transform transform) where T : Component
        {
            var ret = new List<T>();

            Traverse(transform, ret);

            return ret.ToArray();
        }

        static void Traverse<T>(Transform t, List<T> list) where T : Component
        {
            var test = t.GetComponent<T>();
            if (test)
            {
                list.Add(test);
            }
            foreach (Transform child in t)
            {
                Traverse(child, list);
            }
        }

        public static List<T> FindObjectsOfTypeAll<T>()
        {
            List<T> results = new List<T>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.isLoaded)
                {
                    var allGameObjects = s.GetRootGameObjects();
                    for (int j = 0; j < allGameObjects.Length; j++)
                    {
                        var go = allGameObjects[j];
                        results.AddRange(go.GetComponentsInChildren<T>(true));
                    }
                }
            }
            return results;
        }

        public static T GetComponentByInterface<T>(this GameObject gameObject) where T : class
        {
            if (gameObject == null)
            {
                return null;
            }
            return gameObject.GetComponent(typeof(T)) as T;
        }

        public static T[] GetComponentsByInterface<T>(this GameObject gameObject) where T : class
        {
            var components = gameObject.GetComponents(typeof(T));

            var ret = new T[components.Length];

            for (var i = 0; i < ret.Length; i++)
            {
                ret[i] = components[i] as T;
            }

            return ret;
        }

        public static T[] GetComponentsByInterfaceInChildren<T>(this GameObject gameObject, bool includeDisabled = false) where T : class
        {
            var components = gameObject.GetComponentsInChildren(typeof(T), includeDisabled);

            var ret = new T[components.Length];

            for (var i = 0; i < ret.Length; i++)
            {
                ret[i] = components[i] as T;
            }

            return ret;
        }

        public static T GetComponentByInterfaceInChildren<T>(this GameObject gameObject) where T : class
        {
            return gameObject.GetComponentInChildren(typeof(T)) as T;
        }

        public static bool IsNullOrDestroyed(this object target)
        {
            var item = (MonoBehaviour)target;

            return item == null || item.gameObject == null || !item.gameObject.activeInHierarchy;
        }

        public static bool IsNullOrDestroyed(this GameObject target)
        {
            return target == null || target.Equals(null) || !target.activeInHierarchy;
        }

        public static T GetComponentByInterface<T>(this MonoBehaviour component) where T : class
        {
            return component.GetComponent(typeof(T)) as T;
        }

        public static Component[] GetComponentsByInterface<T>(this MonoBehaviour component) where T : class
        {
            return component.GetComponents(typeof(T));
        }

        public static T GetComponentByInterfaceInAncestors<T>(this Transform component) where T : class
        {
            var result = component.GetComponent(typeof(T)) as T;

            if (result != null)
            {
                return result;
            }

            if (component.transform.parent == null)
            {
                return null;
            }

            return GetComponentByInterfaceInAncestors<T>(component.transform.parent);
        }

        public static T[] GetComponentsByInterfaceInAncestors<T>(this Transform component, bool includeInactive = false) where T : class
        {
            var result = component.GetComponentsInParent(typeof(T), includeInactive);
            if (result == null || result.Length == 0)
            {
                return null;
            }

            T[] castResult = new T[result.Length];
            for (int i = 0; i < castResult.Length; i++)
            {
                castResult[i] = result[i] as T;
            }
            return castResult;
        }

        public static T GetComponentInAncestors<T>(this Transform component) where T : Component
        {
            var result = component.GetComponent<T>();

            if (result != null)
            {
                return result;
            }

            if (component.transform.parent == null)
            {
                return null;
            }
            //TODO: Should this be parent?
            return GetComponentInAncestors<T>(component.transform.parent);
        }

        public static void DestroyChildren(this GameObject gameObject)
        {
            foreach (Transform child in gameObject.transform)
            {
                if(Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }

        }

        public static bool BinarySearchCircular<T>(this T[] array, T searchValue, int head, out int lowerIndex, out int upperIndex) where T : IComparable<T>
        {
            int bottom = 0;
            int top = array.Length - 1;
            int count = array.Length;
            int middle = top >> 1;
            while (top >= bottom)
            {
                int middleIndex = (middle + head) % count;
                if (array[middleIndex].CompareTo(searchValue) == 0)
                {
                    upperIndex = middleIndex;
                    lowerIndex = middleIndex;
                    return true;
                }
                if (array[middleIndex].CompareTo(searchValue) > 0)
                {
                    top = middle - 1;
                }
                else
                {
                    bottom = middle + 1;
                }
                middle = (bottom + top) >> 1;
            }
            if (array[head].CompareTo(searchValue) < 0)
            {
                lowerIndex = head;
                upperIndex = -1;
            }
            else if (array[(head + 1) % count].CompareTo(searchValue) > 0)
            {
                upperIndex = (head + 1) % count;
                lowerIndex = -1;
            }
            else
            {
                lowerIndex = (top + head) % count;
                upperIndex = (bottom + head) % count;
            }

            return false;
        }   
    }
}