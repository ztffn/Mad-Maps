using MadMaps.Common.GenericEditor;
using UnityEngine;

namespace MadMaps.Terrains
{
    [Name("Objects/Terrain Surface Object Applier")]
    public class TerrainSurfaceObjectApplier : ProceduralLayerComponent
    {
        public float RequiredY = .9f;

        public override ApplyTiming Timing
        {
            get { return ApplyTiming.OnFrameAfterPostFinalise; }
        }

        public override string HelpURL
        {
            get { return "http://lrtw.net/madmaps/index.php?title=Terrain_Surface_Object"; }
        }

        public override void Apply(ProceduralLayer layer, TerrainWrapper wrapper)
        {
            var obj = wrapper.ObjectContainer;
            if (!obj)
            {
                return;
            }

            wrapper.GetComponent<TerrainCollider>().enabled = false;
            wrapper.GetComponent<TerrainCollider>().enabled = true;

            var children = obj.GetComponentsInChildren<TerrainSurfaceObject>();
            for (int i = 0; i < children.Length; i++)
            {
                var terrainSurfaceObject = children[i];
                if (!terrainSurfaceObject)
                {
                    continue;
                }

                if (terrainSurfaceObject.transform.up.y < RequiredY)
                {
                    continue;
                }
                terrainSurfaceObject.Recalculate();
            }
            Debug.Log(string.Format("Recalculated {0} TerrainSurfaceObjects", children.Length));
        }
    }
}