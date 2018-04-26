using System;
using System.Collections.Generic;
using System.Linq;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Terrains;
using UnityEngine;
using UnityEngine.Serialization;

namespace MadMaps.WorldStamp
{
    public partial class WorldStamp
    {
        public bool VegetationStudioEnabled = true;
        public bool StencilVSData = true;
        public bool RemoveExistingVSData = true;

        public void StampVegetationStudio(TerrainWrapper terrainWrapper, TerrainLayer layer, int stencilKey)
        {
            var tSize = terrainWrapper.Terrain.terrainData.size;
            var tPos = terrainWrapper.transform.position;

            var wrapperBounds =
                new Bounds(terrainWrapper.Terrain.GetPosition() + terrainWrapper.Terrain.terrainData.size / 2,
                    terrainWrapper.Terrain.terrainData.size);
            wrapperBounds.Expand(Vector3.up * 5000);
            if (RemoveExistingVSData)
            {
                var stampBounds = new ObjectBounds(transform.position, Size / 2, transform.rotation);
                stampBounds.Expand(Vector3.up * 5000);
                var compoundVSData = terrainWrapper.GetCompoundVegetationStudioData(layer, true);
                foreach (var vsdataInstance in compoundVSData)
                {
                    if (layer.VSRemovals.Contains(vsdataInstance.Guid))
                    {
                        continue;
                    }

                    var wPos = vsdataInstance.Position;
                    //wPos = new Vector3(wPos.x * tSize.x, wPos.y * tSize.y, wPos.z * tSize.z);
                    wPos += tPos;

                    if (stampBounds.Contains(wPos))
                    {
                        var stencilPos = wPos - tPos;
                        stencilPos = new Vector2(stencilPos.x / tSize.x, stencilPos.z / tSize.z);
                        var stencilAmount = layer.GetStencilStrength(stencilPos, stencilKey);
                        if (stencilAmount > 0.5f)
                        {
                            layer.VSRemovals.Add(vsdataInstance.Guid);
                            //Debug.DrawLine(wPos, wPos + Vector3.up * stencilAmount * 20, Color.red, 30);
                        }
                    }
                }
            }

            if (!VegetationStudioEnabled)
            {
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            for (var i = 0; i < Data.VSData.Count; i++)
            {                
                var vsInstance = Data.VSData[i].Clone();
                var maskPos = new Vector3(vsInstance.Position.x*Data.Size.x, 0, vsInstance.Position.z*Data.Size.z)/* + (Data.Size/2)*/;
                var maskValue = GetMask().GetBilinear(Data.GridManager, maskPos);
                if (maskValue <= 0.25f)
                {
                    continue;
                }

                var wPos = transform.position + transform.rotation * (new Vector3(vsInstance.Position.x * Size.x, vsInstance.Position.y, vsInstance.Position.z * Size.z) - (Size.xz().x0z() / 2));
                if (!wrapperBounds.Contains(wPos))
                {
                    continue;
                }

                if (StencilTrees)
                {
                    var stencilPos = new Vector2((wPos.x - tPos.x) / tSize.x, (wPos.z - tPos.z) / tSize.z);
                    var stencilVal = layer.GetStencilStrength(stencilPos, stencilKey);
                    if (stencilVal <= 0.25f)
                    {
                        continue;
                    }
                }

                vsInstance.Guid = Guid.NewGuid().ToString();
                vsInstance.Position = wPos - terrainWrapper.transform.position;
                vsInstance.Position = new Vector3(vsInstance.Position.x / tSize.x, vsInstance.Position.y / tSize.y - .5f, vsInstance.Position.z / tSize.z);

                layer.VSInstances.Add(vsInstance);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}