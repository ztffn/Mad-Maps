using System;
using MadMaps.Terrains;

namespace MadMaps.Common.Serialization
{
    public static class LegacyManager
    {
        public static Type GetTypeFromLegacy(string name)
        {
            if (name.Contains("HurtTreeInstance"))
            {
                return typeof(MadMapsTreeInstance);
            }
            var t = Type.GetType(name.Replace("sMap", "MadMaps")) ?? Type.GetType(name.Replace("Dingo", "MadMaps"));
            return t;
        }
    }
}