using System;
using UnityEngine;

namespace ActionSystem
{
    [Serializable]
    public class VariableField
    {
        [SerializeField] private bool _useVariable;
        [SerializeField] private bool _useGlobalVariable;

        [SerializeField] private int _variableIndex = -1;

        [SerializeField] private LocalVariableType _directValueType = LocalVariableType.Float;
        [SerializeField] private int _directIntValue;
        [SerializeField] private float _directFloatValue;
        [SerializeField] private bool _directBoolValue;
        [SerializeField] private string _directStringValue;

        public double GetNumber(ActionList context)
        {
            if (_useVariable)
            {
                var variable = GetVariable(context);
                if (variable == null) return 0;

                return variable.Type switch
                {
                    LocalVariableType.Integer => variable.GetInt(),
                    LocalVariableType.Float => variable.GetFloat(),
                    LocalVariableType.Boolean => variable.GetBool() ? 1 : 0,
                    LocalVariableType.String => variable.GetString()?.Length ?? 0,
                    _ => 0
                };
            }

            return _directValueType switch
            {
                LocalVariableType.Integer => _directIntValue,
                LocalVariableType.Float => _directFloatValue,
                LocalVariableType.Boolean => _directBoolValue ? 1 : 0,
                LocalVariableType.String => _directStringValue?.Length ?? 0,
                _ => 0
            };
        }

        private LocalVariable GetVariable(ActionList context)
        {
            if (context == null) return null;

            if (_useGlobalVariable)
                return context.GlobalVariables?.GetVariable(_variableIndex);

            return context.GetVariable(_variableIndex);
        }

        public string GetDisplayName()
        {
            if (_useVariable)
            {
                string prefix = _useGlobalVariable ? "G" : "L";
                return $"{prefix}[{_variableIndex}]";
            }

            return _directValueType switch
            {
                LocalVariableType.Integer => _directIntValue.ToString(),
                LocalVariableType.Float => _directFloatValue.ToString("F1"),
                LocalVariableType.Boolean => _directBoolValue.ToString(),
                LocalVariableType.String => $"\"{_directStringValue}\"",
                _ => "?"
            };
        }
    }
}
