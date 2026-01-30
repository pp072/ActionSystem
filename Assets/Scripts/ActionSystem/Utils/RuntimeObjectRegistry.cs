using System.Collections.Generic;
using UnityEngine;

namespace ActionSystem
{
    /// <summary>
    /// Runtime registry for scene objects that can be referenced by global variables.
    /// Objects register themselves with a string key at runtime.
    /// </summary>
    public class RuntimeObjectRegistry : MonoBehaviour
    {
        private static RuntimeObjectRegistry _instance;
        public static RuntimeObjectRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[RuntimeObjectRegistry]");
                    _instance = go.AddComponent<RuntimeObjectRegistry>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, GameObject> _gameObjects = new();
        private readonly Dictionary<string, Component> _components = new();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterGameObject(string key, GameObject obj)
        {
            if (string.IsNullOrEmpty(key)) return;
            _gameObjects[key] = obj;
        }

        public void RegisterComponent(string key, Component component)
        {
            if (string.IsNullOrEmpty(key)) return;
            _components[key] = component;
        }

        public void Unregister(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _gameObjects.Remove(key);
            _components.Remove(key);
        }

        public GameObject GetGameObject(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return _gameObjects.TryGetValue(key, out var obj) ? obj : null;
        }

        public Component GetComponent(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return _components.TryGetValue(key, out var comp) ? comp : null;
        }

        public T GetComponent<T>(string key) where T : Component
        {
            var comp = GetComponent(key);
            return comp as T;
        }

        public bool HasKey(string key)
        {
            return _gameObjects.ContainsKey(key) || _components.ContainsKey(key);
        }

        public IEnumerable<string> GetAllKeys()
        {
            var keys = new HashSet<string>(_gameObjects.Keys);
            keys.UnionWith(_components.Keys);
            return keys;
        }

        public IEnumerable<string> GetGameObjectKeys() => _gameObjects.Keys;

        public IEnumerable<string> GetComponentKeys() => _components.Keys;

        public void Clear()
        {
            _gameObjects.Clear();
            _components.Clear();
        }
    }
}
