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

    [Serializable, ActionMenuPath("Logic/Action List")]
    public class ActionLists : ActionItemBase
    {
        [SerializeField] private ComponentRef<ActionList> _actionList;
        [SerializeField] private ActionListCommand _actionListCommand;

        public override async UniTask<bool> Run()
        {
            var actionList = _actionList.GetValue(Context);
            if (actionList == null) return true;

            var isSuccess = false;
            switch (_actionListCommand)
            {
                case ActionListCommand.Run:
                    isSuccess = await actionList.Run();
                    break;
                case ActionListCommand.Continue:
                    actionList.ContinueManually();
                    isSuccess = true;
                    break;
                case ActionListCommand.Stop:
                    actionList.Stop();
                    isSuccess = true;
                    break;
                case ActionListCommand.RunAsync:
                    actionList.RunManually();
                    isSuccess = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return isSuccess;
        }
    }
}
