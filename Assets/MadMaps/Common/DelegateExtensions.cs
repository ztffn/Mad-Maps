using System;

namespace MadMaps.Common
{
    public static class DelegateExtensions
    {
        public static void SafeInvoke(this Action action)
        {
            if (action != null)
            {
                action();
            }
        }

        public static void SafeInvoke<T>(this Action<T> action, T obj)
        {
            if (action != null)
            {
                action(obj);
            }
        }
    }
}