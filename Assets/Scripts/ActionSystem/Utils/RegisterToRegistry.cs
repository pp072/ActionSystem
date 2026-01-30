using UnityEngine;

namespace ActionSystem
{
    /// <summary>
    /// Registers this GameObject or a specific Component to the RuntimeObjectRegistry.
    /// Use this to make scene objects accessible to global variables.
    /// </summary>
    public class RegisterToRegistry : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private bool _registerGameObject = true;
        [SerializeField] private Component _componentToRegister;

        public string Key => _key;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_key)) return;

            if (_registerGameObject)
            {
                RuntimeObjectRegistry.Instance.RegisterGameObject(_key, gameObject);
            }

            if (_componentToRegister != null)
            {
                RuntimeObjectRegistry.Instance.RegisterComponent(_key, _componentToRegister);
            }
        }

        private void OnDisable()
        {
            if (string.IsNullOrEmpty(_key)) return;
            RuntimeObjectRegistry.Instance.Unregister(_key);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_key))
            {
                _key = gameObject.name;
            }
        }
    }
}
