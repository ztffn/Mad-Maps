namespace MadMaps.Common
{
    public struct PersistantVal<T>
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

        public PersistantVal(string key, T defaultVal)
        {
            _defaultValue = defaultVal;
            _key = key;
        }

        public static implicit operator T(PersistantVal<T> val)
        {
            return val.Value;
        }
    }
}