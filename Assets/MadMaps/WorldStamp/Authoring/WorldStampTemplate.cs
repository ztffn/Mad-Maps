using MadMaps.Common;
using MadMaps.Common.Collections;
using UnityEngine;

namespace MadMaps.WorldStamps.Authoring
{
#if HURTWORLDSDK
    [StripComponentOnBuild(DestroyGameObject = true)]
#endif
    [HelpURL("http://lrtw.net/madmaps/index.php?title=World_Stamp_Template")]
    public class WorldStampTemplate : MonoBehaviour
    {
        [HideInInspector]
        public Serializable2DFloatArray Mask;
        public Vector3 Size;
        public WorldStampCaptureTemplate Template;

        public void OnDrawGizmos()
        {
            GizmoExtensions.DrawWireCube(transform.position + Vector3.up * (Size.y/2), Size / 2, Quaternion.identity, Color.white);
        }
    }
}