using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPath("Object/Move To")]
    public class ActionMoveTo : ActionItemBase
    {
        [SerializeField] private TransformRef _object;

        [SerializeField] private bool _move;
        [SerializeField, ShowIf(nameof(_move)), AllowNesting]
        private TransformRef _moveTarget;

        [SerializeField] private bool _rotate;
        [SerializeField, ShowIf(nameof(_rotate)), AllowNesting]
        private TransformRef _rotateTarget;

        [SerializeField] private bool _scale;
        [SerializeField, ShowIf(nameof(_scale)), AllowNesting]
        private TransformRef _scaleTarget;

        [SerializeField] private FloatRef _time;
        [SerializeField] private Ease _ease = Ease.Linear;

        public override async UniTask<bool> Run()
        {
            var obj = _object.GetValue(Context);
            if (obj == null) return true;

            var time = _time.GetValue(Context);

            if (_move)
            {
                var moveTarget = _moveTarget.GetValue(Context);
                if (moveTarget != null)
                    obj.DOMove(moveTarget.position, time).SetEase(_ease);
            }
            if (_rotate)
            {
                var rotateTarget = _rotateTarget.GetValue(Context);
                if (rotateTarget != null)
                    obj.DORotate(rotateTarget.rotation.eulerAngles, time).SetEase(_ease);
            }
            if (_scale)
            {
                var scaleTarget = _scaleTarget.GetValue(Context);
                if (scaleTarget != null)
                    obj.DOScale(scaleTarget.localScale, time).SetEase(_ease);
            }

            await Awaitable.WaitForSecondsAsync(time);
            return true;
        }
    }
}
