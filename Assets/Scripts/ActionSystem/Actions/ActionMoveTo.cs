using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPath("Object/Move To")]
    public class ActionMoveTo : ActionItemBase
    {
        [SerializeField] private TransformRef _object;

        [SerializeField] private bool _move;
        [SerializeField, ShowIf(nameof(_move))]
        private TransformRef _moveTarget;

        [SerializeField] private bool _rotate;
        [SerializeField, ShowIf(nameof(_rotate))]
        private TransformRef _rotateTarget;

        [SerializeField] private bool _scale;
        [SerializeField, ShowIf(nameof(_scale))]
        private TransformRef _scaleTarget;

        [SerializeField] private FloatRef _time;
        [SerializeField] private AnimationCurve _ease = AnimationCurve.Linear(0, 0, 1, 1);

        public override async UniTask<bool> Run()
        {
            var obj = _object.GetValue(Context);
            if (obj == null) return true;

            var time = _time.GetValue(Context);

            var startPos = obj.position;
            var startRot = obj.rotation;
            var startScale = obj.localScale;

            Transform moveTarget = _move ? _moveTarget.GetValue(Context) : null;
            Transform rotateTarget = _rotate ? _rotateTarget.GetValue(Context) : null;
            Transform scaleTarget = _scale ? _scaleTarget.GetValue(Context) : null;

            float elapsed = 0f;
            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                float t = _ease.Evaluate(Mathf.Clamp01(elapsed / time));
                if (moveTarget != null) obj.position = Vector3.Lerp(startPos, moveTarget.position, t);
                if (rotateTarget != null) obj.rotation = Quaternion.Lerp(startRot, rotateTarget.rotation, t);
                if (scaleTarget != null) obj.localScale = Vector3.Lerp(startScale, scaleTarget.localScale, t);
                await UniTask.Yield();
            }

            if (moveTarget != null) obj.position = moveTarget.position;
            if (rotateTarget != null) obj.rotation = rotateTarget.rotation;
            if (scaleTarget != null) obj.localScale = scaleTarget.localScale;

            return true;
        }
    }
}
