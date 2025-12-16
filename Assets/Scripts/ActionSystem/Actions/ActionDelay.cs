using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPathAttribute("Time"), ActionName("Delay")]
    public class ActionDelay : IActionItem
    {
        [HideInInspector]public string Name { get; set; } = "Delay";
        [SerializeField] private float Delay;

        public void Init(){}

        public async UniTask<bool> Run()
        {
            await Awaitable.WaitForSecondsAsync(Delay);
            return true;
        }
    }
}