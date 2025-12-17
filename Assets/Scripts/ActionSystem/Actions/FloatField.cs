using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using Variable;

namespace ActionSystem
{
    [Serializable]
    public class FloatField : IField<float>
    {
        public string InputValue;
        public float Value => GetValue();
        public VariablesScriptableObject Vso;
        private float GetValue()
        {
            if (Vso.TryGetFloatVarFromString(InputValue,  out float value))
            {
                return value;
            }

            value =  float.Parse(InputValue);
            return value;
        }
    }
    
    public class IntField : IField<int>
    {
        public string InputValue;
        
        public int Value => GetValue();

        public VariablesScriptableObject Vso;
        private int GetValue()
        {
            if (Vso.TryGetIntVarFromString(InputValue,  out int value))
            {
                return value;
            }

            value =  int.Parse(InputValue);
            return value;
        }
    }
    
    public class StringField : IField<string>
    {
        public string InputValue;
        
        [SerializeField, HideInInspector]
        public string Value => GetValue();

        public VariablesScriptableObject Vso;
        private string GetValue()
        {
            if (Vso.TryGetStringVarFromString(InputValue,  out string value))
            {
                return value;
            }

            value =  InputValue;
            return value;
        }
    }

    public interface IField<out T>
    {
        T Value { get;}
    }
}