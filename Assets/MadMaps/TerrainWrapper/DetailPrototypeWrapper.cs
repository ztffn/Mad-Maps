using System.Collections.Generic;
using UnityEngine;

namespace MadMaps.Terrains
{
    [CreateAssetMenu(menuName = "Mad Maps/Detail Prototype")]
    public class DetailPrototypeWrapper : ScriptableObject
    {
        public float BendFactor;
        public Color DryColor;
        public Color HealthyColor;
        public float MaxHeight;
        public float MaxWidth;
        public float MinHeight;
        public float MinWidth;
        public float NoiseSpread;
        public GameObject Prototype;
        public Texture2D PrototypeTexture;
        public DetailRenderMode RenderMode;

        public DetailPrototype GetPrototype()
        {
            return new DetailPrototype()
            {
                bendFactor = BendFactor,
                dryColor = DryColor,
                healthyColor = HealthyColor,
                maxHeight = MaxHeight,
                maxWidth = MaxWidth,
                minHeight = MinHeight,
                minWidth = MinWidth,
                noiseSpread = NoiseSpread,
                prototype = Prototype,
                prototypeTexture = PrototypeTexture,
                renderMode = RenderMode,
                usePrototypeMesh = (RenderMode == DetailRenderMode.VertexLit)
            };
        }

        public class DetailPrototypeComparer : IEqualityComparer<DetailPrototype>
        {
            public static bool StaticEquals(DetailPrototype x, DetailPrototype y)
            {
                if(x.renderMode == DetailRenderMode.VertexLit)
                {
                    if(x.prototype != y.prototype)
                    {
                        //Debug.Log(string.Format("vertex lit proto: {0} != {1}", x.prototype, y.prototype));
                        return false;
                    }
                }
                else
                {
                    if(x.prototypeTexture != y.prototypeTexture)
                    {
                        //Debug.Log(string.Format("prototypeTexture: {0} != {1}", x.prototypeTexture, y.prototypeTexture));
                        return false;
                    }
                }
                if(x.renderMode != y.renderMode)
                {
                    Debug.Log(string.Format("renderMode: {0} != {1}", x.renderMode, y.renderMode));
                    return false;
                }
                if(x.healthyColor != y.healthyColor)
                {
                    //Debug.Log(string.Format("healthyColor: {0} != {1}", x.healthyColor, y.healthyColor));
                    return false;
                }
                if(x.dryColor != y.dryColor)
                {
                    //Debug.Log(string.Format("dryColor: {0} != {1}", x.dryColor, y.dryColor));
                    return false;
                }
                if(x.maxHeight != y.maxHeight)
                {
                    //Debug.Log(string.Format("maxHeight: {0} != {1}", x.maxHeight, y.maxHeight));
                    return false;
                }
                if(x.minHeight != y.minHeight)
                {
                    //Debug.Log(string.Format("minHeight: {0} != {1}", x.minHeight, y.minHeight));
                    return false;
                }
                if(x.maxWidth != y.maxWidth)
                {
                    //Debug.Log(string.Format("maxWidth: {0} != {1}", x.maxWidth, y.maxWidth));
                    return false;
                }
                if(x.minWidth != y.minWidth)
                {
                    //Debug.Log(string.Format("minWidth: {0} != {1}", x.minWidth, y.minWidth));
                    return false;
                }
                if(x.bendFactor != y.bendFactor)
                {
                    //Debug.Log(string.Format("bendFactor: {0} != {1}", x.bendFactor, y.bendFactor));
                    return false;
                }
                if(x.noiseSpread != y.noiseSpread)
                {
                    //Debug.Log(string.Format("noiseSpread: {0} != {1}", x.noiseSpread, y.noiseSpread));
                    return false;
                }
                return true;
                /*return ((x.renderMode == DetailRenderMode.VertexLit && x.prototype == y.prototype) || x.prototypeTexture == y.prototypeTexture) 
                    && x.renderMode == y.renderMode 
                    && x.healthyColor == y.healthyColor 
                    && x.dryColor == y.dryColor
                    && x.maxHeight == y.maxHeight
                    && x.minHeight == y.minHeight
                    && x.maxWidth == y.maxWidth
                    && x.minWidth == y.minWidth
                    && x.bendFactor == y.bendFactor
                    && x.noiseSpread == y.noiseSpread;*/
            }

            public bool Equals(DetailPrototype x, DetailPrototype y)
            {
                return StaticEquals(x, y);
            }

            public int GetHashCode(DetailPrototype obj)
            {
                int hashCode = 0;
                hashCode = (hashCode * 397) ^ (obj.prototypeTexture != null ? obj.prototypeTexture.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.prototype != null ? obj.prototype.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ obj.bendFactor.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.dryColor.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.healthyColor.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.maxHeight.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.minHeight.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.maxWidth.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.minWidth.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.noiseSpread.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.renderMode.GetHashCode();
                return hashCode;
            }
        }

        public void SetFromPrototype(DetailPrototype prototype)
        {
            BendFactor = prototype.bendFactor;
            DryColor = prototype.dryColor;
            HealthyColor = prototype.healthyColor;
            MaxHeight = prototype.maxHeight;
            MinHeight = prototype.minHeight;
            MaxWidth = prototype.maxWidth;
            MinWidth = prototype.minWidth;
            NoiseSpread = prototype.noiseSpread;
            Prototype = prototype.prototype;
            PrototypeTexture = prototype.prototypeTexture;
            RenderMode = prototype.renderMode;
        }
    }
}