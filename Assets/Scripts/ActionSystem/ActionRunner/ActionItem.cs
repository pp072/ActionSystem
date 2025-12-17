using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    public interface IActionItem
    {
        void Init();
        UniTask<bool> Run();
        string Name { get; }
        void Validate(int index);
    }
}