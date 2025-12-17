using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    public enum ActionListCommand
    {
        Run,
        RunAsync,
        Stop,
        Continue
    }
    [Serializable, ActionMenuPathAttribute("Logic"), ActionName("ActionList")]
    public class ActionLists : IActionItem
    {
        [HideInInspector]public string Name { get; set; } = "ActionList";
        [SerializeField] private ActionList _actionList;
        [SerializeField] private ActionListCommand _actionListCommand;

        public void Validate(int index) { }
        public void Init(){}

        public async UniTask<bool> Run()
        {
            var isSuccess = false;
            switch (_actionListCommand)
            {
                case ActionListCommand.Run:
                    isSuccess = await _actionList.Run();
                    break;
                case ActionListCommand.Continue:
                    _actionList.ContinueManually();
                    break;
                case ActionListCommand.Stop:
                    _actionList.Stop();
                    break;
                case ActionListCommand.RunAsync:
                    isSuccess = true;
                    _actionList.RunManually();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
                
            return isSuccess;
        }
    }
}