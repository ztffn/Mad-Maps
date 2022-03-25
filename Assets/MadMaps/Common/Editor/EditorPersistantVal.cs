namespace MadMaps.Common
{
    /// <summary>
    /// Editor only class that 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct EditorPersistantVal<T>
    {
        private T _defaultValue;
        private string _key;
        public T Value
        {
            get
            {
                return PlayerPrefExtensions.GetVal(_key, _defaultValue);
            }
            set
            {
                PlayerPrefExtensions.SetVal(_key, value);
            }
        }

        public EditorPersistantVal(string key, T defaultVal)
        {
            _defaultValue = defaultVal;
            _key = key;
        }

        public static implicit operator T(EditorPersistantVal<T> val)
        {
            return val.Value;
        }
    }
}