using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MadMaps.Common
{
    public static class ListExtensions
    {
        public static bool IsNullOrEmpty(this IList list)
        {
            if (list == null || list.Count == 0)
            {
                return true;
            }
            for (int i = 0; i < list.Count; i++)
            {
                var variable = list[i];
                if (variable != null)
                {
                    return false;
                }
            }
            return true;
        }

        public static T Random<T>(this IList<T> array)
        {
            if (array.Count == 0)
            {
                throw new Exception("Check for empty arrays before calling this!");
            }
            if (array.Count == 1)
            {
                return array[0];
            }
            return array[UnityEngine.Random.Range(0, array.Count())];
        }

        public static IOrderedEnumerable<T> Randomize<T>(this IList<T> source, int seed = 1324)
        {
            Random rnd = new Random(seed);
            return source.OrderBy((item) => rnd.Next());
        }

        public static void Fill<T>(this Array array, T obj, int count = -1)
        {
            if (count == -1)
            {
                count = array.Length;
            }
            for (var i = 0; i < count; ++i)
            {
                array.SetValue(obj, i);
            }
        }

        public static void Fill<T>(this IList<T> array, T obj, int count = -1)
        {
            if (count == -1)
            {
                count = array.Count;
            }
            var arrayType = array.GetType();
            if (!arrayType.IsArray)
            {
                array.Clear();
            }
            for (var i = 0; i < count; ++i)
            {
                array.Add(obj);
            }
        }

        public static void FillRandom(this float[] array)
        {
            for (var i = 0; i < array.Length; ++i)
            {
                array[i] = UnityEngine.Random.value;
            }
        }

        public static T[] Add<T>(this T[] array, T value)
        {
            var newArray = new T[array.Length + 1];
            for (int i = 0; i < array.Length; i++)
            {
                newArray[i] = array[i];
            }
            newArray[newArray.Length - 1] = value;
            return newArray;
        }

        public static Array Add(this Array array, object value)
        {
            var newArray = Array.CreateInstance(array.GetType().GetElementType(), array.Length + 1);
            for (int i = 0; i < array.Length; i++)
            {
                newArray.SetValue(array.GetValue(i), i);
            }
            newArray.SetValue(value, newArray.Length - 1);
            return newArray;
        }

        public static Array Remove(this Array array, int index)
        {
            var newArray = Array.CreateInstance(array.GetType().GetElementType(), array.Length - 1);
            int copyCounter = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (i == index)
                {
                    continue;
                }
                newArray.SetValue(array.GetValue(i), copyCounter);
                copyCounter++;
            }
            return newArray;
        }

        public static Array Resize(this Array array, int newCount)
        {
            var newArray = new Array[newCount];
            for (int i = 0; i < newCount && i < array.Length; i++)
            {
                newArray.SetValue(array.GetValue(i), i);
            }
            return newArray;
        }
    }
}