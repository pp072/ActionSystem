using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPath("Debug/Comment",  0.5f, 0.5f, 0.3f, 0.5f)]
    public class ActionComment : ActionItemBase
    {
        [SerializeField] 
        private string Comment = "";

        public override void Validate(int index)
        {
            Name = string.IsNullOrEmpty(Comment) ? "Comment" : Comment;
        }

        public override async UniTask<bool> Run()
        {
            return true;
        }
    }
}
