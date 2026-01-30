using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPath("Template/Template")]
    public class ActionTemplate : ActionItemBase
    {
        [SerializeField] private float Delay;

        public override async UniTask<bool> Run()
        {
            await Awaitable.WaitForSecondsAsync(Delay);
            return true;
        }
    }
}
