using System;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using NUnit.Framework.Constraints;
using UnityEngine;
using R3;
using R3.Triggers;
namespace ActionSystem
{
    public enum CollisionType
    {
        Enter,
        Exit,
        Stay
    }
    [Serializable, ActionMenuPathAttribute("Object"), ActionName("Trigger")]
    public class ActionTrigger : IActionItem
    {
        [HideInInspector]public string Name { get; set; } = "Trigger";
        [SerializeField] private GameObject TriggerObject;
        [SerializeField] private bool DetectCollideWithAll = true;
        [SerializeField, HideIf(nameof(DetectCollideWithAll)), AllowNesting] 
        private Collider SpecificCollider;
        [SerializeField] private CollisionType _collisionType = CollisionType.Enter;
        
        private bool _isTriggered = false;
        public void Validate(int index) { }
        public void Init()
        {
            var d = Disposable.CreateBuilder();
            switch (_collisionType)
            {
                case CollisionType.Enter:
                    TriggerObject.OnTriggerEnterAsObservable()
                        .Subscribe(CheckCollide).AddTo(ref d);
                    TriggerObject.OnTriggerExitAsObservable()
                        .Subscribe(CheckNonCollide).AddTo(ref d);
                    break;
                case CollisionType.Exit:
                    TriggerObject.OnTriggerEnterAsObservable()
                        .Subscribe(CheckNonCollide).AddTo(ref d);
                    TriggerObject.OnTriggerExitAsObservable()
                        .Subscribe(CheckCollide).AddTo(ref d);
                    break;
                case CollisionType.Stay:
                    TriggerObject.OnTriggerStayAsObservable()
                        .Subscribe(CheckCollide).AddTo(ref d);
                    TriggerObject.OnTriggerExitAsObservable()
                        .Subscribe(CheckNonCollide).AddTo(ref d);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            d.RegisterTo(TriggerObject.GetCancellationTokenOnDestroy()); // Build and Register
        }

        private void CheckCollide(Collider x)
        {
            if(DetectCollideWithAll || x == SpecificCollider)
                _isTriggered = true;
        }
        private void CheckNonCollide(Collider x)
        {
            if(DetectCollideWithAll || x == SpecificCollider)
                _isTriggered = false;
        }

        public async UniTask<bool> Run()
        {
            await UniTask.WaitUntil(()=>_isTriggered);
            return true;
        }
    }
}