using System;
using NaughtyAttributes;
using UnityEngine;

namespace Actions
{
    public enum VariantType : int
    {
        None = 0, 
        Integer = 1, 
        Boolean = 2, 
        Float = 3, 
        String = 4
    };

    [Serializable]
    public struct Variant
    {
        [HideInInspector, AllowNesting]public bool HasValue;
        [ShowIf(nameof(isInt)), AllowNesting]public int IntValue;
        [ShowIf(nameof(isBool)), AllowNesting] public bool BoolValue;
        [ShowIf(nameof(isFloat)), AllowNesting]public float FloatValue;
        [ShowIf(nameof(isString)), AllowNesting]public string StringValue;
        public VariantType Type;
        
        public bool isBool => Type == VariantType.Boolean;
        public bool isInt => Type == VariantType.Integer;
        public bool isFloat => Type == VariantType.Float;
        public bool isString => Type == VariantType.String;
    }
    
    [Serializable]
    public class VariantParameter
    {
        public Variant Value;
    
        public VariantParameter()
        {
            DefaultValue();
        }
        private void DefaultValue()
        {
            Value.IntValue = -1;
            Value.BoolValue = false;
            Value.FloatValue = 0f;
            Value.StringValue = "";
            Value.HasValue = false;
            Value.Type = VariantType.None;
           
        }
        
        public void SetValue(int value)
        {
            DefaultValue();
            Value.IntValue = value;
            Value.Type = VariantType.Integer;
            Value.HasValue = true;
        }

        public void SetValue(float value)
        {
            DefaultValue();
            Value.FloatValue = value;
            Value.Type = VariantType.Float;
            Value.HasValue = true;
        }

        public void SetValue(bool value)
        {
            DefaultValue();
            Value.BoolValue = value;
            Value.Type = VariantType.Boolean;
            Value.HasValue = true;
        }

        public void SetValue(string value)
        {
            DefaultValue();
            Value.StringValue = value;
            Value.Type = VariantType.String;
            Value.HasValue = true;
        }
        public void SetValue(object value, Type type)
        {
            if(type == typeof(float)) SetValue((float)value);
            else if(type == typeof(int)) SetValue((int)value);
            else if(type == typeof(bool)) SetValue((bool)value);
            else if(type == typeof(string)) SetValue((string)value);
        }
        public void SetValue(object value, VariantType type)
        {
            if(type == VariantType.Float) SetValue((float)value);
            else if(type == VariantType.Integer) SetValue((int)value);
            else if(type == VariantType.Boolean) SetValue((bool)value);
            else if(type == VariantType.String) SetValue((string)value);
        }
        public void SetValue(Variant value)
        {
            Value = value;
            Value.HasValue = true;
        }
        public Variant GetValue()
        {
            return Value;
        }
        public string ValueToString()
        {
            var v= string.Empty;
            if (Value.HasValue)
            {
                if (Value.Type == VariantType.Boolean) v = Value.BoolValue.ToString();
                if (Value.Type == VariantType.Float) v = Value.FloatValue.ToString();
                if (Value.Type == VariantType.Integer) v = Value.IntValue.ToString();
                if (Value.Type == VariantType.String) v = Value.StringValue;
            }
            return v;
        }
        public void ValueFromString(string value, VariantType type)
        {
            if (type == VariantType.Boolean) SetValue(bool.Parse(value));
            if (type == VariantType.Float) SetValue(float.Parse(value));
            if (type == VariantType.Integer) SetValue(int.Parse(value));
            if (type == VariantType.String) SetValue(value);
            this.Value.Type = type;
        }
    }
}