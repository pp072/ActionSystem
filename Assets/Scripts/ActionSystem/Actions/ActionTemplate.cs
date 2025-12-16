using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPathAttribute("Template"), ActionName("Template")]
    public class ActionTemplate : IActionItem
    {
        [HideInInspector]public string Name { get; set; } = "Template";
        [SerializeField] private float Delay;

        public void Init(){}

        public async UniTask<bool> Run()
        {
            await Awaitable.WaitForSecondsAsync(Delay);
            return true;
        }
    }
}