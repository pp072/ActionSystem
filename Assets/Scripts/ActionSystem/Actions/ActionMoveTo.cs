using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPathAttribute("Object"), ActionName("MoveTo")]
    public class ActionMoveTo : IActionItem
    {
        [HideInInspector]public string Name { get; set; } = "MoveTo";
        
        [SerializeField] private Transform Object;
        
        [SerializeField] private bool Move;
        [SerializeField, ShowIf(nameof(Move)), AllowNesting] 
        private Transform MoveRef;
        [SerializeField, ShowIf(nameof(IsShowMoveVector)), AllowNesting] 
        private Vector3 MoveVector;
        
        [SerializeField] private bool Rotate;
        [SerializeField, ShowIf(nameof(Rotate)), AllowNesting]
        private Transform RotateRef;
        [SerializeField, ShowIf(nameof(IsShowRotateVector)), AllowNesting]
        private Vector3 RotateVector;
        
        [SerializeField] private bool Scale;
        [SerializeField, ShowIf(nameof(Rotate)), AllowNesting]
        private Transform ScaleRef;
        [SerializeField, ShowIf(nameof(IsShowScaleVector)), AllowNesting]
        private Vector3 ScaleVector;
        
        [SerializeField] private float Time;
        [SerializeField] private Ease _ease = Ease.Linear;
        
        bool IsShowMoveVector =>Move && MoveRef == null ;
        bool IsShowRotateVector =>Rotate && RotateRef == null ;
        bool IsShowScaleVector =>Scale && ScaleRef == null ;

        private bool isComplete = false;
        public void Validate(int index) { }
        public void Init(){}

        public async UniTask<bool> Run()
        {
            if (Move)
            {
                var newMoveVector = IsShowMoveVector ? MoveVector : MoveRef.position;
                Object.DOMove(newMoveVector, Time).SetEase(_ease);
            }
            if (Rotate)
            {
                var newRotateVector = IsShowRotateVector ? RotateVector : RotateRef.rotation.eulerAngles;
                Object.DORotate(newRotateVector, Time).SetEase(_ease);
            }
            if (Scale)
            {
                var newScaleVector = IsShowScaleVector ? ScaleVector : RotateRef.localScale;
                Object.DOScale(newScaleVector, Time).SetEase(_ease);
            }
            await Awaitable.WaitForSecondsAsync(Time);
            return true;
        }
    }
}