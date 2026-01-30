using System;
using UnityEngine;

namespace ActionSystem
{
    [Serializable]
    public class LocalVariableRef
    {
        [SerializeField] protected int _variableIndex = -1;
        [SerializeField] protected string _variableName = "";
        [SerializeField] protected bool _isGlobalVariable = false;

        public int VariableIndex => _variableIndex;
        public string VariableName => _variableName;
        public bool IsGlobalVariable => _isGlobalVariable;

        public LocalVariable GetVariable(ActionList context)
        {
            if (context == null) return null;

            if (_isGlobalVariable)
                return GetGlobalVariable(context);

            return GetLocalVariable(context);
        }

        protected LocalVariable GetLocalVariable(ActionList context)
        {
            if (context == null) return null;

            var variable = context.GetVariable(_variableIndex);
            if (variable != null && variable.Name == _variableName)
                return variable;

            return context.GetVariable(_variableName);
        }

        protected LocalVariable GetGlobalVariable(ActionList context)
        {
            if (context?.GlobalVariables == null) return null;

            var variable = context.GlobalVariables.GetVariable(_variableIndex);
            if (variable != null && variable.Name == _variableName)
                return variable;

            return context.GlobalVariables.GetVariable(_variableName);
        }
    }

    /// <summary>
    /// Base class for primitive value references (int, float, bool, string)
    /// </summary>
    [Serializable]
    public abstract class ValueRef<T> : LocalVariableRef
    {
        [SerializeField] protected bool _useLocalVariable = false;
        [SerializeField] protected T _directValue;

        public T GetValue(ActionList context)
        {
            if (!_useLocalVariable)
                return _directValue;

            var variable = GetVariable(context);
            if (variable == null) return default;
            return ConvertFromLocal(variable);
        }

        public string GetDisplayName()
        {
            if (!_useLocalVariable)
                return FormatDirectValue(_directValue);

            string prefix = _isGlobalVariable ? "G" : "L";
            return $"{prefix}[{_variableIndex} : {VariableName}]";
        }

        protected virtual string FormatDirectValue(T value) => value?.ToString() ?? "null";

        protected abstract T ConvertFromLocal(LocalVariable variable);
    }

    /// <summary>
    /// Base class for Unity Object references (GameObject, Component)
    /// </summary>
    [Serializable]
    public abstract class ObjectRef<T> : LocalVariableRef where T : UnityEngine.Object
    {
        [SerializeField] protected bool _useLocalVariable = false;
        [SerializeField] protected T _directReference;

        public T GetValue(ActionList context)
        {
            if (!_useLocalVariable)
                return _directReference;

            var variable = GetVariable(context);
            if (variable == null) return null;
            return ConvertFromLocal(variable);
        }

        protected abstract T ConvertFromLocal(LocalVariable variable);
    }

    [Serializable]
    public class GameObjectRef : ObjectRef<GameObject>
    {
        protected override GameObject ConvertFromLocal(LocalVariable variable)
        {
            return variable.Type switch
            {
                LocalVariableType.GameObject => variable.GetGameObject(),
                LocalVariableType.Component => variable.GetComponent()?.gameObject,
                _ => null
            };
        }
    }

    [Serializable]
    public class TransformRef : ObjectRef<Transform>
    {
        protected override Transform ConvertFromLocal(LocalVariable variable)
        {
            return variable.Type switch
            {
                LocalVariableType.GameObject => variable.GetGameObject()?.transform,
                LocalVariableType.Component => variable.GetComponent<Transform>() ?? variable.GetComponent()?.transform,
                _ => null
            };
        }
    }

    [Serializable]
    public class ComponentRef<T> : LocalVariableRef where T : Component
    {
        [SerializeField] private bool _useLocalVariable = false;
        [SerializeField] private T _directReference;

        public T GetValue(ActionList context)
        {
            if (!_useLocalVariable)
                return _directReference;

            var variable = GetVariable(context);
            if (variable == null) return null;

            return variable.Type switch
            {
                LocalVariableType.Component => variable.GetComponent<T>(),
                LocalVariableType.GameObject => variable.GetGameObject()?.GetComponent<T>(),
                _ => null
            };
        }
    }

    [Serializable]
    public class IntRef : ValueRef<int>
    {
        protected override int ConvertFromLocal(LocalVariable variable)
        {
            return variable.Type switch
            {
                LocalVariableType.Integer => variable.GetInt(),
                LocalVariableType.Float => (int)variable.GetFloat(),
                LocalVariableType.Boolean => variable.GetBool() ? 1 : 0,
                _ => default
            };
        }
    }

    [Serializable]
    public class FloatRef : ValueRef<float>
    {
        protected override float ConvertFromLocal(LocalVariable variable)
        {
            return variable.Type switch
            {
                LocalVariableType.Float => variable.GetFloat(),
                LocalVariableType.Integer => variable.GetInt(),
                LocalVariableType.Boolean => variable.GetBool() ? 1f : 0f,
                _ => default
            };
        }

        protected override string FormatDirectValue(float value) => value.ToString("F1");
    }

    [Serializable]
    public class BoolRef : ValueRef<bool>
    {
        protected override bool ConvertFromLocal(LocalVariable variable)
        {
            return variable.Type switch
            {
                LocalVariableType.Boolean => variable.GetBool(),
                LocalVariableType.Integer => variable.GetInt() != 0,
                LocalVariableType.Float => variable.GetFloat() != 0,
                _ => default
            };
        }
    }

    [Serializable]
    public class StringRef : ValueRef<string>
    {
        protected override string ConvertFromLocal(LocalVariable variable)
        {
            return variable.Type switch
            {
                LocalVariableType.String => variable.GetString(),
                _ => variable.GetValue()?.ToString()
            };
        }

        protected override string FormatDirectValue(string value) => $"\"{value ?? ""}\"";
    }
}
