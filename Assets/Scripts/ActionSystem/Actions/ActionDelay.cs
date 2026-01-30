using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPath("Time/Delay")]
    public class ActionDelay : ActionItemBase
    {
        [SerializeField] private FloatRef _delay;

        public override async UniTask<bool> Run()
        {
            var delay = _delay.GetValue(Context);
            await Awaitable.WaitForSecondsAsync(delay);
            return true;
        }
    }
}
