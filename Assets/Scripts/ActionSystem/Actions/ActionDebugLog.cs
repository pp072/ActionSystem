using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPathAttribute("Debug"), ActionName("Debug Log")]
    public class ActionDebugLog : IActionItem
    {
        [HideInInspector]public string Name { get; set; } = "Debug Log";
        [SerializeField] private string Debug;
        [SerializeField] private bool Enable;

        public void Init(){}

        public async UniTask<bool> Run()
        {
            if(Enable)
                UnityEngine.Debug.Log(Debug);
            return true;
        }
    }
}