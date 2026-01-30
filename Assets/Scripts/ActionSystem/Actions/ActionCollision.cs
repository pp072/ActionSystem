using System;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPath("Object/Collision")]
    public class ActionCollision : ActionItemBase
    {
        [SerializeField] 
        private GameObjectRef _collisionObject;
        [SerializeField] 
        private bool _detectCollideWithAll;
        [SerializeField, HideIf(nameof(_detectCollideWithAll)), AllowNesting]
        private ComponentRef<Collider> _specificCollider;
        [SerializeField] 
        private CollisionType _collisionType = CollisionType.Enter;
        [SerializeField, Tooltip("If true, Exit requires Enter first, and Enter requires Exit first")]
        private bool _requireStateTransition;

        private bool _isCollided;
        private bool _hasOppositeState; // Tracks if opposite state occurred (for _requireStateTransition)
        private Collider _resolvedSpecificCollider;
        private bool _resolvedDetectAll;
        private CollisionListener _listener;

        public override void Init()
        {
            _isCollided = false;
            _hasOppositeState = false;

            var resolvedCollisionObject = _collisionObject.GetValue(Context);
            _resolvedSpecificCollider = _specificCollider.GetValue(Context);
            _resolvedDetectAll = _detectCollideWithAll;

            if (resolvedCollisionObject == null) return;

            _listener = ComponentUtils.GetOrAdd<CollisionListener>(resolvedCollisionObject);

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

        private void CheckCollide(Collision collision)
        {
            if (_resolvedDetectAll || collision.collider == _resolvedSpecificCollider)
            {
                // If requireStateTransition is enabled, only trigger if opposite state happened first
                if (!_requireStateTransition || _hasOppositeState)
                    _isCollided = true;
            }
        }

        private void CheckNonCollide(Collision collision)
        {
            if (_resolvedDetectAll || collision.collider == _resolvedSpecificCollider)
            {
                _isCollided = false;
                _hasOppositeState = true; // Mark that opposite state occurred
            }
        }

        public override async UniTask<bool> Run()
        {
            await UniTask.WaitUntil(() => _isCollided);
            return true;
        }
    }
}
