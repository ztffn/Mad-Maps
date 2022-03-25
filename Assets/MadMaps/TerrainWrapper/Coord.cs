using System;
using UnityEngine;

namespace MadMaps.Common
{
    [Serializable]
    public struct Coord
    {
        public bool Equals(Coord other)
        {
            return x == other.x && z == other.z;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x*397) ^ z;
            }
        }

        public static Coord One { get { return new Coord(1, 1);} }
        public static Coord Zero { get { return new Coord(0, 0);} }

        public readonly int x;
        public readonly int z;

        public Coord(int xVal, int zVal)
        {
            x = xVal;
            z = zVal;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", x, z);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Coord && Equals((Coord) obj);
        }

        public static bool operator ==(Coord first, Coord second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(Coord first, Coord second)
        {
            return !(first == second);
        }

        public static Coord operator +(Coord first, Coord second)
        {
            return new Coord(first.x + second.x, first.z + second.z);
        }

        public static Coord operator -(Coord first, Coord second)
        {
            return new Coord(first.x - second.x, first.z - second.z);
        }

        public Coord Clamp(int p1, int p2)
        {
            return new Coord(Mathf.Clamp(x, p1, p2), Mathf.Clamp(z, p1, p2));
        }
    }
}