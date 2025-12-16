using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace ActionSystem
{
    [Serializable, ActionMenuPathAttribute("Event"), ActionName("Send Event")]
    public class ActionSetEvent : IActionItem
    {
        public string Name { get; set; } = "Send Event";
        [SerializeField] private UnityEvent _floatDrivenTarget;
        public void Init()
        {
            
        }

        public UniTask<bool> Run()
        {
            _floatDrivenTarget.Invoke();
            return UniTask.FromResult(true);
        }
    }
}