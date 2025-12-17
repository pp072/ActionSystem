using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace ActionSystem
{
    [Serializable]
    public class ActionList : MonoBehaviour
    {
        [SerializeField] private bool _runOnAwake = false;
        [SerializeField] private List<Action> Actions = new();
        private bool Pause =  false;
        private bool Stopped = false;

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
        
        [Button()]
        public void ExpandAll()
        {
            //var listProp = serializedObject.FindProperty("_actionItems");
        }
        
        public void Stop()
        {
            Stopped = true;
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
                action.Init();
            }

            if (_runOnAwake)
                RunManually();
        }

        private void OnValidate()
        {
            for (var index = 0; index < Actions.Count; index++)
            {
                var action = Actions[index];
                if (action.IsFinishTypeGoTo)
                {
                    action.ActionNodesList.Clear();
                    for (var i = 0; i < Actions.Count; i++)
                    {
                        var actionInList = Actions[i];
                        var actionInfo = new ActionInfo
                        {
                            index = i,
                            name = actionInList.ActionName
                        };
                        action.ActionNodesList.Add(actionInfo);
                    }
                }
                action.Validate(index);
            }
        }
    }
}
