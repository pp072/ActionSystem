using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPath("Object/Enable Disable")]
    public class ActionEnableObject : ActionItemBase
    {
        [SerializeField] private GameObjectRef _gameObject;
        [SerializeField] private BoolRef _enable;

        public override async UniTask<bool> Run()
        {
            var go = _gameObject.GetValue(Context);
            var enable = _enable.GetValue(Context);

            if (go != null)
                go.SetActive(enable);
            return true;
        }
    }
}