using System;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace ActionSystem
{
    [Serializable]
    public enum ActionVariableCommand
    {
        Set,
        Get,
        Modify
    }

    [Serializable, ActionMenuPath("Variable/Get Set")]
    public class ActionVariable : ActionItemBase
    {
        [SerializeField, AllowNesting]
        private LocalVariableRef _targetVariable;

        [SerializeField] private ActionVariableCommand _command;

        [SerializeField, ShowIf(nameof(IsSetInt)), AllowNesting]
        private IntRef _setIntVal;

        [SerializeField, ShowIf(nameof(IsSetFloat)), AllowNesting]
        private FloatRef _setFloatVal;

        [SerializeField, ShowIf(nameof(IsSetBool)), AllowNesting]
        private BoolRef _setBoolVal;

        [SerializeField, ShowIf(nameof(IsSetString)), AllowNesting]
        private StringRef _setStringVal;

        [SerializeField, ShowIf(nameof(IsModifyCommand)), AllowNesting]
        private string _expression;

        [SerializeField, HideInInspector]
        private LocalVariableType _cachedVariableType;

        private bool IsSetCommand => _command == ActionVariableCommand.Set;
        private bool IsModifyCommand => _command == ActionVariableCommand.Modify;
        private bool IsSetInt => IsSetCommand && _cachedVariableType == LocalVariableType.Integer;
        private bool IsSetFloat => IsSetCommand && _cachedVariableType == LocalVariableType.Float;
        private bool IsSetBool => IsSetCommand && _cachedVariableType == LocalVariableType.Boolean;
        private bool IsSetString => IsSetCommand && _cachedVariableType == LocalVariableType.String;

        private LocalVariable GetTargetVariable()
        {
            return _targetVariable?.GetVariable(Context);
        }

        public override void Validate(int index)
        {
            base.Validate(index);

            // Auto-detect variable type from target
            var variable = _targetVariable?.GetVariable(Context);
            if (variable != null)
            {
                _cachedVariableType = variable.Type;
            }
        }

        public override async UniTask<bool> Run()
        {
            var variable = GetTargetVariable();
            if (variable == null) return true;

            switch (_command)
            {
                case ActionVariableCommand.Get:
                    LogVariable(variable);
                    break;

                case ActionVariableCommand.Set:
                    SetVariable(variable);
                    LogVariable(variable);
                    break;

                case ActionVariableCommand.Modify:
                    ModifyVariable(variable);
                    LogVariable(variable);
                    break;
            }

            return true;
        }

        private void SetVariable(LocalVariable variable)
        {
            switch (_cachedVariableType)
            {
                case LocalVariableType.Integer:
                    variable.SetValue(_setIntVal.GetValue(Context));
                    break;
                case LocalVariableType.Float:
                    variable.SetValue(_setFloatVal.GetValue(Context));
                    break;
                case LocalVariableType.Boolean:
                    variable.SetValue(_setBoolVal.GetValue(Context));
                    break;
                case LocalVariableType.String:
                    variable.SetValue(_setStringVal.GetValue(Context));
                    break;
            }
        }

        private void ModifyVariable(LocalVariable variable)
        {
            switch (_cachedVariableType)
            {
                case LocalVariableType.Float:
                {
                    var finalExpression = variable.GetFloat() + _expression;
                    ExpressionEvaluator.Evaluate(finalExpression, out float result);
                    variable.SetValue(result);
                    break;
                }
                case LocalVariableType.Integer:
                {
                    var finalExpression = variable.GetInt() + _expression;
                    ExpressionEvaluator.Evaluate(finalExpression, out float result);
                    variable.SetValue((int)result);
                    break;
                }
            }
        }

        private void LogVariable(LocalVariable variable)
        {
            Debug.Log($"Variable: {variable}");
        }
    }
}
