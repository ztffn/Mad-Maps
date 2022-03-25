using System;
using UnityEngine;

namespace MadMaps.Common.GenericEditor
{
	public class NameAttribute : Attribute 
	{
		public string Name;

		public NameAttribute(string name){
			Name = name;
		}
	}
	
	public class ShowIfAttribute : Attribute 
	{
		public string FieldName;
		public bool Invert;

		public ShowIfAttribute(string fieldName, bool invert = false)
        {
			FieldName = fieldName;
			Invert = invert;
		}
	}
	
	public class ListGenericUIAttribute : Attribute 
	{
		public bool Reorderable;
		public bool AllowDerived;
	}

    public class InternalManagedType : Attribute
    {
    }

    public class MinAttribute : PropertyAttribute
    {
        public float MinValue;

        public MinAttribute(float minValue)
        {
            MinValue = minValue;
        }
    }

    public class MaxAttribute : PropertyAttribute
    {
        public float MaxValue;

        public MaxAttribute(float maxValue)
        {
            MaxValue = maxValue;
        }
    }
}