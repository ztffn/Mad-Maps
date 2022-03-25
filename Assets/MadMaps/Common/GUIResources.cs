using UnityEngine;

namespace MadMaps.Common
{
    public static partial class GUIResources
    {
        public static Font OpenSans_ExtraBold
        {
            get
            {
                if (__openSans_ExtraBold == null)
                {
                    __openSans_ExtraBold = Resources.Load<Font>("MadMaps/OpenSans/OpenSans-ExtraBold");
                }
                return __openSans_ExtraBold;
            }
        }
        private static Font __openSans_ExtraBold;
        public static Texture2D PopoutIcon
        {
            get
            {
                if (__popoutIcon == null)
                {
                    __popoutIcon = Resources.Load<Texture2D>("MadMaps/PopoutIcon");
                }
                return __popoutIcon;
            }
        }
        private static Texture2D __popoutIcon;

        public static Texture2D WarningIcon
        {
            get
            {
                if (__warningIcon == null)
                {
                    __warningIcon = Resources.Load<Texture2D>("MadMaps/WarningIcon");
                }
                return __warningIcon;
            }
        }
        private static Texture2D __warningIcon;

        public static Texture2D EyeOpenIcon
        {
            get
            {
                if (__eyeOpenIcon == null)
                {
                    __eyeOpenIcon = Resources.Load<Texture2D>("MadMaps/EyeOpenIcon");
                }
                return __eyeOpenIcon;
            }
        }
        private static Texture2D __eyeOpenIcon;

        public static Texture2D LockedIcon
        {
            get
            {
                if (__lockedIcon == null)
                {
                    __lockedIcon = Resources.Load<Texture2D>("MadMaps/LockedIcon");
                }
                return __lockedIcon;
            }
        }
        private static Texture2D __lockedIcon;

        public static Texture2D UnlockedIcon
        {
            get
            {
                if (__unlockedIcon == null)
                {
                    __unlockedIcon = Resources.Load<Texture2D>("MadMaps/UnlockedIcon");
                }
                return __unlockedIcon;
            }
        }
        private static Texture2D __unlockedIcon;
    }
}