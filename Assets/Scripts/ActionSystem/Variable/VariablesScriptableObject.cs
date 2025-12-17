using System;
using System.Collections.Generic;
using System.Linq;
using Actions;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

namespace Variable
{
    [Serializable]
    public class Variable
    {
        public string Name;
        [ReadOnly , AllowNesting]public int index;
        public VariantParameter Var;
        public Variant StartValue; 
    }
    public class VariablesScriptableObject : ScriptableObject
    {
        public List<Variable> Variables = new List<Variable>();

        private void OnValidate()
        {
            for (var index = 0; index < Variables.Count; index++)
            {
                var v = Variables[index];
                v.index = index;
                v.StartValue.Type = v.Var.Value.Type;
            }
            ResetValues();
        }

        private void OnEnable()
        {
            ResetValues();
        }

        private void ResetValues()
        {
            foreach (var variable in Variables)
                variable.Var.SetValue(variable.StartValue);
        }
        
        string ExtractBetween(string text, char start, char end)
        {
            int i = text.IndexOf(start);
            if (i < 0) return null;

            int j = text.IndexOf(end, i + 1);
            if (j < 0) return null;

            return text.Substring(i + 1, j - i - 1);
        }

        private Variant? TryGetVariant(string input)
        {
            if (input is { Length: > 2 } str &&
                str.Contains('[') && str.Contains(']'))
            {
                var inner = ExtractBetween(input, '[', ']');

                if (!string.IsNullOrEmpty(inner))
                {
                    if (inner.StartsWith("Var:"))
                        inner = inner[4..];
                    
                    if (int.TryParse(inner, out int f))
                    {
                        var val = Variables.FirstOrDefault(x => x.index == f);
                        if (val != null) return val.Var.Value;
                    }
                }
            }
            return null;
        }
        public bool TryGetFloatVarFromString(string input, out float value)
        {
            if (TryGetVariant(input) is { Type: VariantType.Float } var)
            {
                value =  var.FloatValue;
                return true;
            }

            value = 0;
            return false;
        }
        public bool TryGetIntVarFromString(string input, out int value)
        {
            if (TryGetVariant(input) is { Type: VariantType.Integer } var)
            {
                value =  var.IntValue;
                return true;
            }

            value = 0;
            return false;
        }
        public bool TryGetStringVarFromString(string input, out string value)
        {
            if (TryGetVariant(input) is { Type: VariantType.String } var)
            {
                value =  var.StringValue;
                return true;
            }

            value = "";
            return false;
        }
    }
}