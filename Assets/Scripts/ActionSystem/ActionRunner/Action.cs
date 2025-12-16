using System;
using System.Collections.Generic;
using NaughtyAttributes;
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

    public struct ActionInfo
    {
        public int index;
        public string name;
    }
    
    [Serializable]
    public class Action
    {
        [SerializeReference, HideInInspector]
        private string _name = "";
        
        [SerializeReference, SerializeReferenceDropdown, DisableIf(nameof(IsSkip)), AllowNesting ]
        private IActionItem _actionItem;
        
        [SerializeField] 
        private RunType RunType = RunType.Wait;
        
        [SerializeField, ShowIf(nameof(IsRunTypeWaitUntil)), AllowNesting] 
        private FinishType FinishType = FinishType.Continue;

        [SerializeField, Dropdown(nameof(GetAllActionsInfo)), ShowIf(nameof(IsFinishTypeGoTo)), AllowNesting]
        private int GoTo;

        public bool IsSkip => RunType == RunType.Skip;
        public bool IsRunTypeWaitUntil => RunType == RunType.Wait;
        public bool IsFinishTypeGoTo => FinishType ==  FinishType.GoTo;
        public IActionItem ActionItem => _actionItem;
        public RunType GetRunType => RunType;
        public FinishType GetFinishType => FinishType;
        public int GetGoTo => GoTo;
        public string ActionName => _name;
        
        public List<ActionInfo> ActionNodesList { get; } = new List<ActionInfo>();
        
        public DropdownList<int> GetAllActionsInfo()
        {
            var list = new DropdownList<int>();
            foreach (var parameter in ActionNodesList)
            {
                list.Add(parameter.name 
                    , parameter.index);
            }
            return list;
        }
        public void Validate(int index)
        {
            if(_actionItem == null) return;
            _name = index + ": " + _actionItem.Name + (RunType == RunType.Skip ? "  SKIP" :"")  ;
        }
        
        public void Init()
        {
            _actionItem.Init();
        }

        public void SetInProgressState(bool state)
        {
            _name = state ? _name.Replace(":", "* :") : _name.Replace("* :", ":");
        }
    }
}