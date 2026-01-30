using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPath("Debug/Debug Log")]
    public class ActionDebugLog : ActionItemBase
    {
        [SerializeField] private StringRef Debug;
        [SerializeField] private bool Enable;

        public override async UniTask<bool> Run()
        {
            var text = Debug.GetValue(Context);
            if(Enable)
                UnityEngine.Debug.Log(text);
            return true;
        }
    }
}
