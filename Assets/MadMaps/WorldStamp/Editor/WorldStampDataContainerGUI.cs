using UnityEditor;
using UnityEngine;
using MadMaps.Common;

namespace MadMaps.WorldStamps
{
    /*[InitializeOnLoad]
    public class WorldStampDataPreserver
	{
        static WorldStampDataPreserver()
        {
            UnityEditor.PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdate;
        }

	    static void OnPrefabInstanceUpdate(GameObject instance)
		{
            GameObject prefab = UnityEditor.PrefabUtility.GetPrefabParent(instance) as GameObject;
            if(!prefab.GetComponent<WorldStamp>())
            {
                return;
            }
            var prefabData = prefab.GetComponentInChildren<WorldStampDataContainer>();
            if(!prefabData)
            {
                Debug.LogError(string.Format("Couldn't find data for prefab {0} - it might be lost.", prefab), prefab);
                return;
            }
            Debug.Log(string.Format("Prefab heights are: {0}", prefabData.Data.Heights.Width));
            var instanceData = instance.GetComponentInChildren<WorldStampDataContainer>();
            if(!instanceData)
            {
                Debug.LogError(string.Format("Couldn't find data for instance {0} - it might be lost.", instance), instance);
                return;
            }
            instanceData.Redirect = null;
            instanceData.Data = prefabData.Data.JSONClone();
            WorldStampDataContainer.Ignores.Add(instanceData.Data);
            Debug.Log(string.Format("Set instance data to prefab data. Instance heights are: {0}", prefabData.Data.Heights.Width), instanceData);
        }
	}*/

    [CustomEditor(typeof(WorldStampDataContainer))]
    [CanEditMultipleObjects]
    public class WorldStampDataContainerGUI : Editor
    {

        public static void TurnIntoProxy(WorldStampDataContainer wsdc)
        {

            if (!EditorUtility.DisplayDialog("Are you sure?",
                "This will delete any instance data you changed on this object.", "Yes", "No"))
            {
                return;
            }

#if UNITY_2018_2_OR_NEWER
            var prefabParent = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(wsdc.transform.parent.gameObject);
#else
            var prefabParent = UnityEditor.PrefabUtility.GetPrefabParent(wsdc.transform.parent.gameObject);
#endif
            if (prefabParent == null)
            {
                Debug.LogError("Unable to find prefab");
                return;
            }
            var prefabGo = prefabParent as GameObject;
            if (prefabGo == null)
            {
                Debug.LogError("Unable to find prefab");
                return;
            }
            var prefabWsdc = prefabGo.GetComponentInChildren<WorldStampDataContainer>();
            if (prefabWsdc == null)
            {
                Debug.LogError("Unable to find data container on prefab");
                return;
            }
            if(wsdc.Redirect != prefabWsdc)
            {
                wsdc.Redirect = prefabWsdc;
                if(WorldStampDataContainer.Ignores.Contains(wsdc.Data))
                {
                    WorldStampDataContainer.Ignores.Remove(wsdc.Data);
                    prefabWsdc.Data = wsdc.Data.JSONClone();
                }
            }
#if HURTWORLDSDK
            if(wsdc.Redirect != null && !ReferenceEquals(wsdc.Redirect.Data, wsdc.Data))
            {
                //Debug.Log("Set instance data to null, but this should be fine.", wsdc);
                wsdc.Data = null;
            }
#endif
        }

        public static void TurnIntoInstance(WorldStampDataContainer wsdc)
        {
            wsdc.Data = JsonUtility.FromJson<WorldStampData>(JsonUtility.ToJson(wsdc.Redirect.Data));
            wsdc.Redirect = null;
        }

        protected override void OnHeaderGUI()
        {
        }
    }
}