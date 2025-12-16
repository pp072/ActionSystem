using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPathAttribute("Object"), ActionName("Enable Disable Object")]
    public class ActionEnableObject : IActionItem
    {
        [HideInInspector]public string Name { get; set; } = "Enable Disable Object";
        [SerializeField] private GameObject _gameObject;
        [SerializeField] private bool Enable = true;

        public void Init()
        {
            
        }

        public async UniTask<bool> Run()
        {
            if(_gameObject!=null)
                _gameObject.SetActive(Enable);
            return true;
        }
    }
}