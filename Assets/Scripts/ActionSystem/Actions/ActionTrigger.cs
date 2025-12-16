using System;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private CollisionType _collisionType = CollisionType.Enter;
        
        private bool _isTriggered = false;

        public void Init()
        {
            var d = Disposable.CreateBuilder();
            switch (_collisionType)
            {
                case CollisionType.Enter:
                    TriggerObject.OnTriggerEnterAsObservable()
                        .Subscribe(x => { _isTriggered = true; }).AddTo(ref d);
                    TriggerObject.OnTriggerExitAsObservable()
                        .Subscribe(x => { _isTriggered = false; }).AddTo(ref d);
                    break;
                case CollisionType.Exit:
                    TriggerObject.OnTriggerEnterAsObservable()
                        .Subscribe(x => { _isTriggered = false; }).AddTo(ref d);
                    TriggerObject.OnTriggerExitAsObservable()
                        .Subscribe(x => { _isTriggered = true; }).AddTo(ref d);
                    break;
                case CollisionType.Stay:
                    TriggerObject.OnTriggerStayAsObservable()
                        .Subscribe(x => { _isTriggered = true; }).AddTo(ref d);
                    TriggerObject.OnTriggerExitAsObservable()
                        .Subscribe(x => { _isTriggered = false; }).AddTo(ref d);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            d.RegisterTo(TriggerObject.GetCancellationTokenOnDestroy()); // Build and Register
        }

        public async UniTask<bool> Run()
        {
            await UniTask.WaitUntil(()=>_isTriggered);
            return true;
        }
    }
}