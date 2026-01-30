using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    public interface IActionItem
    {
        void Init();
        void Init(ActionList context);
        void Cleanup();
        UniTask<bool> Run();
        string Name { get; }
        void Validate(int index);
        ActionList Context { get; set; }
    }

    [Serializable]
    public abstract class ActionItemBase : IActionItem
    {
        [HideInInspector] public string Name { get; set; }
        protected LocalVariable GetLocalVariable(string name) => Context?.GetVariable(name);
        protected LocalVariable GetLocalVariable(int index) => Context?.GetVariable(index);
        protected T GetLocalValue<T>(string name) => Context != null ? Context.GetValue<T>(name) : default;

        public ActionList Context { get; set; }

        public virtual void Init() { }

        public void Init(ActionList context)
        {
            Context = context;
            Init();
        }

        public virtual void Cleanup() { }

        public abstract UniTask<bool> Run();

        public virtual void Validate(int index)
        {
            if (string.IsNullOrEmpty(Name))
            {
                var attr = GetType()
                    .GetCustomAttributes(typeof(ActionMenuPathAttribute), false)
                    .FirstOrDefault() as ActionMenuPathAttribute;
                Name = attr?.Name ?? GetType().Name;
            }
        }
    }
}
