using System;
using NaughtyAttributes;
using UnityEngine;
using Variable;

namespace Actions
{
    [Serializable]
    public class VariableField
    {
        [SerializeField]private bool _varNotFromList;
        [Dropdown(nameof(GetAllVariables)), AllowNesting]
        [SerializeField, HideIf(nameof(_varNotFromList))]private Variable.Variable _varFromList;
        [SerializeField, ShowIf(nameof(_varNotFromList)), AllowNesting]private string _var;
        [SerializeField, ShowIf(nameof(_varNotFromList)), AllowNesting]public VariantType type;
    
        VariablesScriptableObject VSO => VariablesScriptableObject.instance;
        public DropdownList<Variable.Variable> GetAllVariables()
        {
            var list = new DropdownList<Variable.Variable>();
            foreach (var parameter in VSO.Variables)
            {
                list.Add($"[Var:{parameter.index}] {parameter.Name} {parameter.Var.Value.Type.ToString()} {parameter.Var.ValueToString()} " 
                    , parameter);
            }
            return list;
        }

        public Variant GetValue()
        {
            if (!_varNotFromList)
                return _varFromList.Var.Value;
        
            var variant = new VariantParameter();
            variant.ValueFromString(_var,  type);
            return variant.Value;
        }

        public double GetNumber()
        {
            var value = GetValue();
            switch (value.Type)
            {
                case VariantType.Integer:
                    return value.IntValue;
                case VariantType.Float:
                    return value.FloatValue;
                case VariantType.String:
                    return value.StringValue.Length;
                case VariantType.Boolean:
                    return value.BoolValue ? 1 : 0;
                default:
                    return 0;
            }
        }
    }
}