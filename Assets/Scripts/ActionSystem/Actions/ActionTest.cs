using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPathAttribute("Test"), ActionName("Test")]
    public class ActionTest : IActionItem
    {
        [HideInInspector]public string Name { get; set; } = "Enable Disable Object";
        [SerializeField] private GameObject _gameObject;
        [SerializeField] private Color _color;
        [SerializeField] private bool Enable = true;
        [SerializeField] private FloatField  _floatField;
        public void Validate(int index) { }
        public void Init()
        {
            
        }

        public async UniTask<bool> Run()
        {
            Debug.Log(_floatField.Value);
            //_gameObject.SetActive(Enable);
            return true;
        }
    }
}