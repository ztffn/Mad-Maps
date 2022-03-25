using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if HURTWORLDSDK
[StripComponentOnBuild]
#endif
[HelpURL("http://lrtw.net/madmaps/index.php?title=Terrain_Surface_Object")]
public class TerrainSurfaceObject : MonoBehaviour
{
    [Serializable]
    public struct CastConfig
    {
        public Vector3 Position;
        public float AcceptableDistance;
    }
    
    [HideInInspector]
    public List<CastConfig> CastPoints = new List<CastConfig>();
    public int RequireCasts = 1;
    public LayerMask Mask = ~0;
    public bool IsDebug;
    public const float CastDist = 100;

    public void Start()
    {
        // Just to get the enabled flag to show up
    }

    [ContextMenu("Recalculate")]
    public void Recalculate()
    {
        if (!enabled)
        {
            return;
        }
        
        var pos = transform.position;
        var rot = transform.rotation;
        float minY = transform.position.y;
        int successCount = 0;
        foreach (var castConfig in CastPoints)
        {
            var castOffset = castConfig.Position;
            castOffset = Vector3.Scale(castOffset, transform.localScale);
            var acceptableDist = castConfig.AcceptableDistance*transform.localScale.y;
            var castPos = pos + rot * castOffset;
            var yDelta = castPos.y - transform.position.y;
            var castDist = CastDist*transform.localScale.y;

            var hits = Physics.RaycastAll(castPos, Vector3.down, castDist, Mask);
            if (hits.Length == 0)
            {
                if (IsDebug)
                {
                    Debug.DrawLine(castPos, castPos + Vector3.down * castDist, Color.red, 10);
                }
                continue;
            }

            hits = hits.OrderBy(raycastHit => raycastHit.distance).ToArray();
            for (int i = 0; i < hits.Length; i++)
            {
                var raycastHit = hits[i];
                if (raycastHit.collider.transform == transform)
                {
                    continue;
                }
                if (raycastHit.distance <= acceptableDist)
                {
                    if (IsDebug)
                    {
                        Debug.DrawLine(castPos, raycastHit.point, Color.green, 10);
                    }
                    successCount++;
                    break;
                }
                var candidateMinY = raycastHit.point.y + acceptableDist - yDelta;
                if (candidateMinY  < minY)
                {
                    minY = candidateMinY;
                }

                if (IsDebug)
                {
                    Debug.DrawLine(castPos, raycastHit.point, Color.white, 10);
                }
                break;
            }
        }

        if (successCount >= RequireCasts)
        {
            return;
        }

        transform.position = new Vector3(transform.position.x, minY, transform.position.z);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.up, out hit, CastDist, 1 << 21))
        {
            var mmObjAbove = hit.collider.gameObject.GetComponent<TerrainSurfaceObject>();
            if (mmObjAbove != null && !mmObjAbove.Equals(null))
            {
                if (ReferenceEquals(mmObjAbove, this))
                {
                    Debug.LogError("Hit self");
                    return;
                }
                mmObjAbove.Recalculate();
            }
        }
    }
    
    public bool Serialize
    {
        get { return true; }
    }
}
