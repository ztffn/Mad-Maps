using System;
using System.Collections.Generic;
using UnityEngine;

namespace MadMaps.Terrains
{
    [Serializable]
    public class MadMapsTreeInstance
    {
        public Color Color;
        public string Guid;
        public Vector3 Position;
        public GameObject Prototype;
        public Vector2 Scale;

        public MadMapsTreeInstance(Vector3 position, Vector2 scale, GameObject prototype, Color color)
        {
            //Wrapper = terrainWrapper;
            Guid = System.Guid.NewGuid().ToString();
            Position = position;
            Scale = scale;
            Prototype = prototype;
            Color = color;
        }

        public MadMapsTreeInstance(TreeInstance instance, List<TreePrototype> treePrototypes)
        {
            //Wrapper = wrapper;
            Position = instance.position;
            Color = instance.color;
            Scale = new Vector2(instance.widthScale, instance.heightScale);
            Prototype = treePrototypes[instance.prototypeIndex].prefab;
            Guid = System.Guid.NewGuid().ToString();
        }

        public TreeInstance ToUnityTreeInstance(List<TreePrototype> prototypes)
        {
            var prototypeIndex = prototypes.FindIndex(prototype => prototype.prefab == Prototype);
            if (prototypeIndex < 0)
            {
                prototypes.Add(new TreePrototype { prefab = Prototype });
                prototypeIndex = prototypes.Count - 1;
            }
            return new TreeInstance
            {
                position = Position,
                color = Color,
                heightScale = Scale.y,
                widthScale = Scale.x,
                prototypeIndex = prototypeIndex,
                lightmapColor = Color.white,
                rotation = 0
            };
        }
        /*
        public void AutoPlace(TerrainWrapper wrapper, LayerMask castMask)
        {
            if (wrapper == null)
            {
                Debug.LogWarning("Wrapper was null!");
                return;
            }
            var worldPos = wrapper.Terrain.TreeToWorldPos(Position);
            const float castDistance = 20;
            RaycastHit hit;
            if (!Physics.Raycast(worldPos, Vector3.down, out hit, castDistance, castMask))
            {
                Debug.DrawLine(worldPos, worldPos + Vector3.down * castDistance, Color.red, 5);
                return;
            }
            Position = wrapper.Terrain.WorldToTreePos(hit.point);
            Debug.DrawLine(worldPos, hit.point, Color.green, 5);
        }
        */
        public MadMapsTreeInstance Clone()
        {
            return new MadMapsTreeInstance(Position, Scale, Prototype, Color)
            {
                Guid = Guid,
            };
        }
    }
}