using System.Collections.Generic;
using UnityEngine;

namespace MadMaps.Terrains
{
    [CreateAssetMenu(menuName = "Mad Maps/Splat Prototype")]
    public partial class SplatPrototypeWrapper : ScriptableObject
    {
        public Texture2D Texture;
        public Texture2D NormalMap;
        public float Smoothness;
        public Color SpecularColor;
        public Vector2 TileSize = new Vector2(15, 15);
        public Vector2 TileOffset;
        public float Metallic;
        public float Multiplier = 1;
#if HURTWORLDSDK
        public EMaterialType Material;
#endif

        public SplatPrototype GetPrototype()
        {
            return new SplatPrototype()
            {
                texture = Texture,
                normalMap = NormalMap,
                metallic = Metallic,
                smoothness = Smoothness,
                specular = SpecularColor,
                tileOffset = TileOffset,
                tileSize = TileSize,
            };
        }

        public void SetFromPrototype(SplatPrototype prototype)
        {
            Texture = prototype.texture;
            NormalMap = prototype.normalMap;
            Smoothness = prototype.smoothness;
            SpecularColor = prototype.specular;
            TileSize = prototype.tileSize;
            TileOffset = prototype.tileOffset;
            Metallic = prototype.metallic;
        }

        public class SplatPrototypeComparer : IEqualityComparer<SplatPrototype>
        {
            public static bool StaticEquals(SplatPrototype x, SplatPrototype y)
            {
                return x.texture == y.texture
                    && x.normalMap == y.normalMap
                    && x.smoothness == y.smoothness
                    && x.specular == y.specular
                    && x.metallic == y.metallic
                    && x.tileSize == y.tileSize
                    && x.tileOffset == y.tileOffset;
            }

            public bool Equals(SplatPrototype x, SplatPrototype y)
            {
                return StaticEquals(x, y);
            }

            public int GetHashCode(SplatPrototype obj)
            {
                int hashCode = 0;
                hashCode = (hashCode * 397) ^ (obj.texture != null ? obj.texture.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.normalMap != null ? obj.normalMap.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ obj.smoothness.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.specular.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.tileSize.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.tileOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.metallic.GetHashCode();
                return hashCode;
            }
        }
    }
}