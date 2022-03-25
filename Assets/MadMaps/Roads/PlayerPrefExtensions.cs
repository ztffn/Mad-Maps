using UnityEngine;

namespace MadMaps.Common
{
    public static class PlayerPrefExtensions
    {
        public static void SetVal<T>(string key, T val)
        {
            var t = typeof (T);
            if (t == typeof (string))
            {
                PlayerPrefs.SetString(key, val as string);
                return;
            }
            if (t == typeof(int))
            {
                PlayerPrefs.SetInt(key, (int)(object)val);
                return;
            }
            if (t == typeof(float))
            {
                PlayerPrefs.SetFloat(key, (float)(object)val);
                return;
            }
            if (t == typeof(bool))
            {
                var castVar = (bool) (object) val;
                PlayerPrefs.SetInt(key, castVar ? 1 : 0);
                return;
            }

            var json = JsonUtility.ToJson(val);
            PlayerPrefs.SetString(key, json);
        }

        public static T GetVal<T>(string key, T defaultVal)
        {
            if (string.IsNullOrEmpty(key))
            {
                return defaultVal;
            }
            var t = typeof(T);
            if (t == typeof(string))
            {
                return (T)(object)PlayerPrefs.GetString(key, defaultVal as string);
            }
            if (t == typeof(int))
            {
                return (T)(object)PlayerPrefs.GetInt(key, (int)(object)defaultVal);
            }
            if (t == typeof(float))
            {
                return (T)(object)PlayerPrefs.GetFloat(key, (float)(object)defaultVal);
            }
            if (t == typeof(bool))
            {
                var defaultBool = (bool) (object) defaultVal;
                var intValue = PlayerPrefs.GetInt(key, defaultBool ? 1 : 0);
                return (T)(object)(intValue == 1);
            }

            if (PlayerPrefs.HasKey(key))
            {
                var jsonString = PlayerPrefs.GetString(key);
                T result = JsonUtility.FromJson<T>(jsonString);
                if (result != null)
                {
                    return result;
                }
            }
            return defaultVal;
        }
    }
}