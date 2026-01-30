using System;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace ActionSystem
{
    public enum CollisionType
    {
        Enter,
        Exit,
        Stay
    }

    [Serializable, ActionMenuPath("Object/Trigger")]
    public class ActionTrigger : ActionItemBase
    {
        [SerializeField] private GameObjectRef _triggerObject;
        [SerializeField] private bool _detectCollideWithAll;
        [SerializeField, HideIf(nameof(_detectCollideWithAll)),AllowNesting] 
        private ComponentRef<Collider> _specificCollider;
        [SerializeField] private CollisionType _collisionType = CollisionType.Enter;
        [SerializeField, Tooltip("If true, Exit requires Enter first, and Enter requires Exit first")]
        private bool _requireStateTransition;

        private bool _isTriggered;
        private bool _hasOppositeState;
        private Collider _resolvedSpecificCollider;
        private bool _resolvedDetectAll;
        private TriggerListener _listener;

        public override void Init()
        {
            _isTriggered = false;
            _hasOppositeState = false;

            var resolvedTriggerObject = _triggerObject.GetValue(Context);
            _resolvedSpecificCollider = _specificCollider.GetValue(Context);
            _resolvedDetectAll = _detectCollideWithAll;

            if (resolvedTriggerObject == null) return;

            _listener = ComponentUtils.GetOrAdd<TriggerListener>(resolvedTriggerObject);

            switch (_collisionType)
            {
                case CollisionType.Enter:
                    _listener.OnEnter += CheckCollide;
                    _listener.OnExit += CheckNonCollide;
                    break;
                case CollisionType.Exit:
                    _listener.OnEnter += CheckNonCollide;
                    _listener.OnExit += CheckCollide;
                    break;
                case CollisionType.Stay:
                    _listener.OnStay += CheckCollide;
                    _listener.OnExit += CheckNonCollide;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Cleanup()
        {
            if (_listener == null) return;

            switch (_collisionType)
            {
                case CollisionType.Enter:
                    _listener.OnEnter -= CheckCollide;
                    _listener.OnExit -= CheckNonCollide;
                    break;
                case CollisionType.Exit:
                    _listener.OnEnter -= CheckNonCollide;
                    _listener.OnExit -= CheckCollide;
                    break;
                case CollisionType.Stay:
                    _listener.OnStay -= CheckCollide;
                    _listener.OnExit -= CheckNonCollide;
                    break;
            }

            _listener = null;
        }

        private void CheckCollide(Collider x)
        {
            if (_resolvedDetectAll || x == _resolvedSpecificCollider)
            {
                if (!_requireStateTransition || _hasOppositeState)
                    _isTriggered = true;
            }
        }

        private void CheckNonCollide(Collider x)
        {
            if (_resolvedDetectAll || x == _resolvedSpecificCollider)
            {
                _isTriggered = false;
                _hasOppositeState = true;
            }
        }

        public override async UniTask<bool> Run()
        {
            await UniTask.WaitUntil(() => _isTriggered);
            return true;
        }
    }
}
