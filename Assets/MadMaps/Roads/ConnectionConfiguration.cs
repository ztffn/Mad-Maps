using System;
using System.Collections.Generic;
using MadMaps.Common.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

namespace MadMaps.Roads
{
    [CreateAssetMenu(menuName = "Mad Maps/Road Connection Configuration")]
    [HelpURL("http://lrtw.net/madmaps/index.php?title=Connection_Configuration")]
    public class ConnectionConfiguration : ScriptableObject, ISerializationCallbackReceiver
    {
        public const float DefaultCurviness = 60;

        [Tooltip("The color this connection will show in the Scene window.")]
        [FormerlySerializedAs("Color")]
        public Color PreviewColor = UnityEngine.Color.white;
        [Tooltip("The curviness of the splines with this configuration.")]
        public float Curviness = DefaultCurviness;
        
        public List<ConnectionConfigurationBase> Components = new List<ConnectionConfigurationBase>();

        [HideInInspector]
        [SerializeField]
        private List<Common.Serialization.DerivedComponentJsonDataRow> ComponentsJSON = new List<Common.Serialization.DerivedComponentJsonDataRow>();
        
        public void OnBeforeSerialize()
        {
            ComponentsJSON.Clear();
            foreach (var connectionComponentConfigBase in Components)
            {
                var t = connectionComponentConfigBase.GetType();
                var jsonRow = new Common.Serialization.DerivedComponentJsonDataRow()
                {
                    AssemblyQualifiedName = t.AssemblyQualifiedName,
                };
                jsonRow.JsonText = JSONSerializer.Serialize(t, connectionComponentConfigBase, false,
                    jsonRow.SerializedObjects);
                ComponentsJSON.Add(jsonRow);
            }
        }

        public void OnAfterDeserialize()
        {
            Components.Clear();
            foreach (var row in ComponentsJSON)
            {
                var type = Type.GetType(row.AssemblyQualifiedName) ?? LegacyManager.GetTypeFromLegacy(row.AssemblyQualifiedName);
                
                if (type == null)
                {
                    Debug.LogError("Couldn't get type " + row.AssemblyQualifiedName);
                }

                Components.Add(
                    (ConnectionConfigurationBase)
                        JSONSerializer.Deserialize(type, row.JsonText,
                            row.SerializedObjects));
            }
        }

        public ConnectionConfigurationBase GetComponentWithGUID(string componentGuid)
        {
            for (int i = 0; i < Components.Count; i++)
            {
                var component = Components[i];
                if (component.GUID == componentGuid)
                {
                    return component;
                }
            }
            return null;
        }
    }
}
