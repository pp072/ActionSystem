using System;
using NaughtyAttributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ActionSystem
{
    public enum LocalVariableType
    {
        GameObject,
        Component,
        Integer,
        Float,
        Boolean,
        String
    }

    [Serializable]
    public class LocalVariable
    {
        [SerializeField] private string _name = "Variable";
        [SerializeField] private LocalVariableType _type = LocalVariableType.GameObject;

        [SerializeField, ShowIf(nameof(IsGameObject)), AllowNesting]
        private GameObject _gameObjectValue;

        [SerializeField, ShowIf(nameof(IsComponent)), AllowNesting]
        private Component _componentValue;

        [SerializeField, ShowIf(nameof(IsInteger)), AllowNesting]
        private int _intValue;

        [SerializeField, ShowIf(nameof(IsFloat)), AllowNesting]
        private float _floatValue;

        [SerializeField, ShowIf(nameof(IsBoolean)), AllowNesting]
        private bool _boolValue;

        [SerializeField, ShowIf(nameof(IsString)), AllowNesting]
        private string _stringValue;

        // Registry key for global variables (ScriptableObject can't hold scene references)
        // Only shown via custom drawer in ScriptableObject context
        [SerializeField, HideInInspector]
        private string _registryKey;

        // Condition properties for ShowIf
        private bool IsGameObject => _type == LocalVariableType.GameObject;
        private bool IsComponent => _type == LocalVariableType.Component;
        private bool IsInteger => _type == LocalVariableType.Integer;
        private bool IsFloat => _type == LocalVariableType.Float;
        private bool IsBoolean => _type == LocalVariableType.Boolean;
        private bool IsString => _type == LocalVariableType.String;

        public string Name => _name;
        public LocalVariableType Type => _type;
        public string RegistryKey => _registryKey;

        public GameObject GetGameObject()
        {
            // Try direct reference first, then fall back to registry
            if (_gameObjectValue != null)
                return _gameObjectValue;

            if (!string.IsNullOrEmpty(_registryKey))
                return RuntimeObjectRegistry.Instance.GetGameObject(_registryKey);

            return null;
        }

        public Component GetComponent()
        {
            // Try direct reference first, then fall back to registry
            if (_componentValue != null)
                return _componentValue;

            if (!string.IsNullOrEmpty(_registryKey))
                return RuntimeObjectRegistry.Instance.GetComponent(_registryKey);

            return null;
        }

        public T GetComponent<T>() where T : Component
        {
            // Try direct reference first
            if (_componentValue is T typed)
                return typed;

            if (!string.IsNullOrEmpty(_registryKey))
                return RuntimeObjectRegistry.Instance.GetComponent<T>(_registryKey);

            return null;
        }
        public int GetInt() => _intValue;
        public float GetFloat() => _floatValue;
        public bool GetBool() => _boolValue;
        public string GetString() => _stringValue;

        public Object GetObjectValue()
        {
            return _type switch
            {
                LocalVariableType.GameObject => _gameObjectValue,
                LocalVariableType.Component => _componentValue,
                _ => null
            };
        }

        public object GetValue()
        {
            return _type switch
            {
                LocalVariableType.GameObject => _gameObjectValue,
                LocalVariableType.Component => _componentValue,
                LocalVariableType.Integer => _intValue,
                LocalVariableType.Float => _floatValue,
                LocalVariableType.Boolean => _boolValue,
                LocalVariableType.String => _stringValue,
                _ => null
            };
        }

        public void SetValue(GameObject value)
        {
            _type = LocalVariableType.GameObject;
            _gameObjectValue = value;
        }

        public void SetValue(Component value)
        {
            _type = LocalVariableType.Component;
            _componentValue = value;
        }

        public void SetValue(int value)
        {
            _type = LocalVariableType.Integer;
            _intValue = value;
        }

        public void SetValue(float value)
        {
            _type = LocalVariableType.Float;
            _floatValue = value;
        }

        public void SetValue(bool value)
        {
            _type = LocalVariableType.Boolean;
            _boolValue = value;
        }

        public void SetValue(string value)
        {
            _type = LocalVariableType.String;
            _stringValue = value;
        }

        public override string ToString()
        {
            var valueStr = _type switch
            {
                LocalVariableType.GameObject => _gameObjectValue != null ? _gameObjectValue.name : "null",
                LocalVariableType.Component => _componentValue != null ? $"{_componentValue.GetType().Name}" : "null",
                LocalVariableType.Integer => _intValue.ToString(),
                LocalVariableType.Float => _floatValue.ToString("F2"),
                LocalVariableType.Boolean => _boolValue.ToString(),
                LocalVariableType.String => _stringValue ?? "null",
                _ => "unknown"
            };
            return $"[{_name}] ({_type}) = {valueStr}";
        }
    }
}
