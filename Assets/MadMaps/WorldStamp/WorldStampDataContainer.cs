using UnityEngine;
using MadMaps.Common;
using System.Collections.Generic;

namespace MadMaps.WorldStamps
{
    #if HURTWORLDSDK
    [StripComponentOnBuild]
    #endif
    public class WorldStampDataContainer : MonoBehaviour
    {
        public static HashSet<WorldStampData> Ignores = new HashSet<WorldStampData>();

        public WorldStampData GetData()
        {
            if (Redirect == this)
            {
                Redirect = null;
            }
            if (Redirect != null && Redirect.Data != null)
            {
                return Redirect.GetData();
            }
            return Data;
        }

        [HideInInspector]
        public WorldStampData Data;
        public WorldStampDataContainer Redirect;

        public void LinkToPrefab()
        {
#if UNITY_EDITOR
#if UNITY_2018_2_OR_NEWER
            var prefabParent = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(transform.parent.gameObject);
#else
            var prefabParent = UnityEditor.PrefabUtility.GetPrefabParent(transform.parent.gameObject);
#endif
            if (prefabParent == null)
            {
                //Debug.LogError("Unable to find prefab");
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
            if(Redirect == null || Redirect.Data != prefabWsdc.Data)
            {
                Redirect = prefabWsdc;
                if(Ignores.Contains(Data))
                {
                    Ignores.Remove(Data);
                    prefabWsdc.Data = Data.JSONClone();
                }
#if HURTWORLDSDK
                if(!ReferenceEquals(Data, prefabWsdc.Data))
                {
                    //Debug.Log("Set data to null but this should be fine.");
                    Data = null;
                }
#endif
            }            
#endif
        }
    }
}