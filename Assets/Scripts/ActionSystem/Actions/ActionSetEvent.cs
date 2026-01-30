using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace ActionSystem
{
    [Serializable, ActionMenuPath("Event/Send Event")]
    public class ActionSetEvent : ActionItemBase
    {
        [SerializeField] private UnityEvent _floatDrivenTarget;

        public override UniTask<bool> Run()
        {
            _floatDrivenTarget.Invoke();
            return UniTask.FromResult(true);
        }
    }
}
