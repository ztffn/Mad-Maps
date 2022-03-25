using System;
using MadMaps.Common.GenericEditor;
using UnityEngine;

namespace MadMaps.Roads
{
    public abstract class ConnectionConfigurationBase
    {
        [Common.GenericEditor.Min(1)]
        public int Priority = 1;
        public bool Enabled = true;
        
        public override string ToString()
        {
            return GenericEditor.GetFriendlyName(GetType().DeclaringType);
        }

        public string GUID
        {
            get
            {
                if (string.IsNullOrEmpty(__guid))
                {
                    __guid = System.Guid.NewGuid().ToString();
                }
                return __guid;
            }
        }
        [SerializeField]
        private string __guid;

        public abstract Type GetMonoType();
    }
}