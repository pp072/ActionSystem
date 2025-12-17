using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    [Serializable, ActionMenuPathAttribute("Debug"), ActionName("Comment")]
    public class ActionComment : IActionItem
    {
        [SerializeField]private string Comment = "";
        [HideInInspector]public string Name { get; set; } = "Comment";
        public void Validate(int index)
        {
            Name = Comment;
        }

        public void Init(){}

        public async UniTask<bool> Run()
        {
            return true;
        }
    }
}