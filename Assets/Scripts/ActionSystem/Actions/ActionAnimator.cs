using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPath("Animation/Animator")]
    public class ActionAnimator : ActionItemBase
    {
        [SerializeField] private ComponentRef<Animator> _animator;
        [SerializeField] private StringRef _stateName;
        [SerializeField] private FloatRef _transitionDuration;
        [SerializeField] private IntRef _layer;
        [SerializeField] private FloatRef _timeOffset;
        [SerializeField] private bool _waitForTransition;
        [SerializeField] private bool _waitForAnimation;

        public override async UniTask<bool> Run()
        {
            var animator = _animator.GetValue(Context);
            if (animator == null) return true;

            var stateName = _stateName.GetValue(Context);
            var transitionDuration = _transitionDuration.GetValue(Context);
            var layer = _layer.GetValue(Context);
            var timeOffset = _timeOffset.GetValue(Context);

            animator.CrossFadeInFixedTime(stateName, transitionDuration, layer, timeOffset);

            if (_waitForAnimation)
            {
                // Wait for transition to complete, then wait for animation to finish
                await Awaitable.WaitForSecondsAsync(transitionDuration);
                await UniTask.WaitUntil(() => animator.GetCurrentAnimatorStateInfo(layer).normalizedTime >= 1f);
            }
            else if (_waitForTransition)
            {
                await Awaitable.WaitForSecondsAsync(transitionDuration);
            }

            return true;
        }
    }
}
