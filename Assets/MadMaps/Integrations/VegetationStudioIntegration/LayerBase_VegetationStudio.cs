using System.Collections.Generic;
using MadMaps.Common.Collections;
using MadMaps.WorldStamps;
using UnityEngine;

#if VEGETATION_STUDIO
using AwesomeTechnologies;

namespace MadMaps.Terrains
{
    public partial class LayerBase
    {
        public virtual List<VegetationStudioInstance> GetVegetationStudioData()
        {
            return null;
        }

        public virtual List<string> GetVegetationStudioRemovals()
        {
            return null;
        }

        public virtual List<VegetationPackage> GetPackages()
        {
            return null;
        }
    }
}
#endif