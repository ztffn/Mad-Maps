﻿using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Dingo.Common.Serialization
{
    [Serializable]
    public class DerivedComponentJsonDataRow
    {
        public string AssemblyQualifiedName;
        public string JsonText;
        public List<Object> SerializedObjects = new List<Object>();
    }
}