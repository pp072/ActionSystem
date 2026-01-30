using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using Variable;

namespace ActionSystem
{
    [Serializable]
    public class ActionList : MonoBehaviour
    {
        [SerializeField] private bool _runOnAwake = false;
        [SerializeField] private VariablesScriptableObject _globalVariables;
        [SerializeField] private List<LocalVariable> _localVariables = new();
        [SerializeField] private List<Action> Actions = new();
        private bool Pause =  false;
        private bool Stopped = false;

        public IReadOnlyList<LocalVariable> LocalVariables => _localVariables;
        public VariablesScriptableObject GlobalVariables => _globalVariables;
        public IReadOnlyList<Action> ActionsList => Actions;

        public LocalVariable GetVariable(string name)
        {
            foreach (var variable in _localVariables)
            {
                if (variable.Name == name)
                    return variable;
            }
            return null;
        }

        public LocalVariable GetVariable(int index)
        {
            if (index >= 0 && index < _localVariables.Count)
                return _localVariables[index];
            return null;
        }

        public T GetValue<T>(string name)
        {
            var variable = GetVariable(name);
            if (variable == null) return default;

            var value = variable.GetValue();
            if (value is T typedValue)
                return typedValue;
            return default;
        }

        [Button()]
        public void RunManually()
        {
            Run().Forget();
        }
        
        [Button()]
        public void ContinueManually()
        {
            Pause = false;
        }
        
        public void Stop()
        {
            Stopped = true;
            CleanupAllActions();
        }

        private void CleanupAllActions()
        {
            foreach (var action in Actions)
            {
                action.ActionItem?.Cleanup();
            }
        }
        
        public async UniTask<bool> Run(int index=0)
        {
            Debug.Log($"ActionListRun: {gameObject.name}", gameObject);
            for (var i = index; i < Actions.Count; i++)
            {
                if (Stopped)
                {
                    Stopped = false;
                    return true;
                }
                
                var action = Actions[i];
                var isComplete = false;

                switch (action.GetRunType)
                {
                    case RunType.Wait:
                        action.SetInProgressState(true);
                        isComplete = await action.ActionItem.Run();
                        action.SetInProgressState(false);

                        // Check for flow control action (like If/Else)
                        if (action.ActionItem is IFlowControlAction flowControl)
                        {
                            int nextIndex = flowControl.GetNextIndex();
                            if (nextIndex == (int)FlowControlResult.Stop)
                                return true;
                            if (nextIndex >= 0)
                            {
                                Run(nextIndex).Forget();
                                return true;
                            }
                            // Continue = -1, just continue to next action
                            break;
                        }

                        switch (action.GetFinishType)
                        {
                            case FinishType.Stop:
                                return true;
                            case FinishType.Pause:
                                Pause = true;
                                await UniTask.WaitUntil(() => !Pause);
                                break;
                            case FinishType.GoTo:
                                Run(action.GetGoTo).Forget();
                                return true;
                        }

                        break;
                    case RunType.NotWait:
                        action.SetInProgressState(true);
                        action.ActionItem.Run().Forget();
                        action.SetInProgressState(false);
                        break;
                    case RunType.Skip:
                        continue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (!isComplete)
                {
                    Debug.LogError($"Action list failed! {action.ActionName}", gameObject);
                    return false;
                }
            }

            return true;
        }

        private void Start()
        {
            foreach (var action in Actions)
            {
                action.Init(this);
            }

            if (_runOnAwake)
                RunManually();
        }

        private void OnDestroy()
        {
            CleanupAllActions();
        }

        private void OnValidate()
        {
            // Build action info list for dropdowns
            var actionInfoList = new List<ActionInfo>();
            for (var i = 0; i < Actions.Count; i++)
            {
                actionInfoList.Add(new ActionInfo
                {
                    index = i,
                    name = Actions[i].ActionName
                });
            }

            for (var index = 0; index < Actions.Count; index++)
            {
                var action = Actions[index];

                // Set context for editor-time validation
                if (action.ActionItem != null)
                    action.ActionItem.Context = this;

                // Populate ActionNodesList for GoTo dropdown
                if (action.IsFinishTypeGoTo)
                {
                    action.ActionNodesList.Clear();
                    action.ActionNodesList.AddRange(actionInfoList);
                }

                // Populate ActionNodesList for IFlowControlAction (like If/Else)
                if (action.ActionItem is IFlowControlAction)
                {
                    var flowAction = action.ActionItem as ActionIfElse;
                    if (flowAction != null)
                    {
                        flowAction.ActionNodesList.Clear();
                        flowAction.ActionNodesList.AddRange(actionInfoList);
                    }
                }

                action.Validate(index);
            }
        }
    }
}
