using System;
using System.Collections.Generic;
using UnityEngine;

namespace ActionSystem
{
    public enum RunType
    {
        Wait,
        NotWait,
        Skip
    }
    public enum FinishType
    {
        Continue,
        Stop,
        Pause,
        GoTo
    }

    [Serializable]
    public class Action
    {
        [SerializeReference, HideInInspector]
        private string _name = "";

        [SerializeReference, SerializeReferenceDropdown]
        private IActionItem _actionItem;

        [Space]
        [SerializeField]
        private bool AdvancedRunType = false;

        [SerializeField]
        private RunType RunType = RunType.Wait;

        [SerializeField]
        private FinishType FinishType = FinishType.Continue;

        [SerializeField]
        private int GoTo;

        public bool IsSkip => RunType == RunType.Skip;
        public bool IsRunTypeWaitUntil => RunType == RunType.Wait && AdvancedRunType;
        public bool IsFinishTypeGoTo => FinishType == FinishType.GoTo;
        public bool IsFlowControlAction => _actionItem is IFlowControlAction;
        public IActionItem ActionItem => _actionItem;
        public bool IsInProgress { get; private set; }
        public RunType GetRunType => RunType;
        public FinishType GetFinishType => FinishType;
        public int GetGoTo => GoTo;
        public void SetGoTo(int value) => GoTo = value;
        public string ActionName => _name;

        public List<ActionInfo> ActionNodesList { get; } = new List<ActionInfo>();

        public void Validate(int index)
        {
            if(_actionItem == null) return;

            // Validate action item first to populate Name
            _actionItem.Validate(index);

            var goTo = "";
            if (IsFinishTypeGoTo)
            {
                foreach (var actionInfo in ActionNodesList)
                {
                    if (actionInfo.index == GetGoTo)
                    {
                        goTo = " Go to -> " +" "+ actionInfo.name;
                    }
                }
            }

            _name = index + ": " + _actionItem.Name + (RunType == RunType.Skip ? "  SKIP" :"") + goTo;
        }
        
        public void Init(ActionList context)
        {
            _actionItem.Init(context);
        }

        public void SetInProgressState(bool state)
        {
            IsInProgress = state;
        }
    }
}