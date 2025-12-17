using System;
using System.Linq;
using Actions;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using Variable;

namespace ActionSystem
{
    [Serializable]
    public enum ActionVariableCommand
    {
        Set,
        Get,
        Modificate
    }
    [Serializable, ActionMenuPathAttribute("Variable"), ActionName("Variable Get Set")]
    public class ActionVariable : IActionItem
    {
        [HideInInspector]public string Name { get; set; } = "Variable Get Set";
        [SerializeField]private VariablesScriptableObject VSO;
        [Dropdown(nameof(GetAllVariables)), AllowNesting]
        [SerializeField]private Variable.Variable _variable;
        [SerializeField]private ActionVariableCommand _command;
        [SerializeField, ShowIf(nameof(IsIntValue)) , AllowNesting]
        private int _setIntVal;
        [SerializeField, ShowIf(nameof(IsBoolValue)), AllowNesting]
        private int _setBoolVal;
        [SerializeField, ShowIf(nameof(IsFloatValue)), AllowNesting]
        private int _setFloatVal;
        [SerializeField, ShowIf(nameof(IsStringValue)), AllowNesting]
        private int _setStringVal;
        [SerializeField, ShowIf(nameof(_command), ActionVariableCommand.Modificate), AllowNesting] 
        private string _expression;
        
        private bool IsSetCommand => _command == ActionVariableCommand.Set;
        private bool IsIntValue => IsSetCommand && _variable.Var.Value.Type == VariantType.Integer;
        private bool IsBoolValue => IsSetCommand && _variable.Var.Value.Type == VariantType.Boolean;
        private bool IsFloatValue => IsSetCommand && _variable.Var.Value.Type == VariantType.Float;
        private bool IsStringValue => IsSetCommand && _variable.Var.Value.Type == VariantType.String;

        

        public DropdownList<Variable.Variable> GetAllVariables()
        {
            var list = new DropdownList<Variable.Variable>();
            if (VSO == null)
                return null;
            foreach (var parameter in VSO.Variables)
            {
                list.Add($"[Var:{parameter.index}] {parameter.Name} {parameter.Var.Value.Type.ToString()} {parameter.Var.ValueToString()} " 
                    , parameter);
            }
            return list;
        }

        public void Validate(int index)
        {
            if (VSO != null) return;
            VariablesScriptableObject _instance = null;
#if UNITY_EDITOR
            var guid = AssetDatabase.FindAssets($"t:{typeof(VariablesScriptableObject).Name}").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                _instance = AssetDatabase.LoadAssetAtPath<VariablesScriptableObject>(path);
            }
#else
                _instance =  Resources.Load<VariablesScriptableObject>("Prefabs");
#endif
            
            if (_instance != null)
            {
                VSO = _instance;
            }
        }
        public void Init() {}
        
        public VariantParameter Modificate(VariantParameter data)
        {
            switch (data.Value.Type)
            {
                case VariantType.Float:
                {
                    var finalExpression = data.Value.FloatValue + _expression;
                    ExpressionEvaluator.Evaluate(finalExpression, out float result);
                    data.SetValue(result);
                    break;
                }
                case VariantType.Integer:
                {
                    var finalExpression = data.Value.IntValue + _expression;
                    ExpressionEvaluator.Evaluate(finalExpression, out float result);
                    data.SetValue(result);
                    break;
                }
                default:
                    return data;
            }
            return data;
        }
        
        public async UniTask<bool> Run()
        {
            switch (_command)
            {
                case ActionVariableCommand.Get:
                    LogVariable();
                    break;
                case ActionVariableCommand.Set:
                    switch (_variable.Var.Value.Type)
                    {
                        case VariantType.Integer:
                            _variable.Var.SetValue(_setIntVal);
                            LogVariable();
                            break;
                        case VariantType.Boolean:
                            _variable.Var.SetValue(_setBoolVal);
                            LogVariable();
                            break;
                        case VariantType.Float:
                            _variable.Var.SetValue(_setFloatVal);
                            LogVariable();
                            break;
                        case VariantType.String:
                            _variable.Var.SetValue(_setStringVal);
                            LogVariable();
                            break;
                        case VariantType.None:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case ActionVariableCommand.Modificate:
                {
                    Modificate(_variable.Var);
                    LogVariable();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        private void LogVariable()
        {
            Debug.Log($"Var:{_variable.index} {_variable.Name}  {_variable.Var.ValueToString()}");
        }
    }
}