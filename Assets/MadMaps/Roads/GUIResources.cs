using UnityEngine;

namespace MadMaps.Common
{
    public static partial class GUIResources
    {
        public static Texture2D RoadConfigurationIcon
        {
            get
            {
                if (__roadConfigurationIcon == null)
                {
                    __roadConfigurationIcon = Resources.Load<Texture2D>("MadMaps/RoadConfigurationIcon");
                }
                return __roadConfigurationIcon;
            }
        }
        private static Texture2D __roadConfigurationIcon;

        public static Texture2D IntersectionIcon
        {
            get
            {
                if (__intersectionIcon == null)
                {
                    __intersectionIcon = Resources.Load<Texture2D>("IntersectionIcon");
                }
                return __intersectionIcon;
            }
        }
        private static Texture2D __intersectionIcon;

        public static Texture2D NodeIcon
        {
            get
            {
                if (__nodeIcon == null)
                {
                    __nodeIcon = Resources.Load<Texture2D>("NodeIcon");
                }
                return __nodeIcon;
            }
        }
        private static Texture2D __nodeIcon;

        public static Texture2D RoadNetworkIcon
        {
            get
            {
                if (__roadNetworkIcon == null)
                {
                    __roadNetworkIcon = Resources.Load<Texture2D>("RoadNetworkIcon");
                }
                return __roadNetworkIcon;
            }
        }
        private static Texture2D __roadNetworkIcon;
    }
}