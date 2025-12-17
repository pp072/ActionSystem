using System;
using Actions;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace ActionSystem
{
    public enum ActionIfElseCommand
    { 
        Greater,
        Lower,
        Equal,
        NotEqual,
        GreaterOrEqual,
        LowerOrEqual,
    }
    [Serializable, ActionMenuPathAttribute("Variable"), ActionName("Variable If Else")]
    public class ActionIfElse : IActionItem
    {
        [HideInInspector]public string Name { get; set; } = "Variable If Else";
        
        [InfoBox("IF", EInfoBoxType.Normal)]
        [BoxGroup("Variable 1"), AllowNesting] [SerializeField]
        private VariableField var1;
        
        [SerializeField] private ActionIfElseCommand _command;
        
        [BoxGroup("Variable 2"), AllowNesting] [SerializeField]
        private VariableField var2;
        
        [SerializeField] private ActionList _TRUE;
        [SerializeField] private ActionListCommand _actionListTrueCommand;
        [SerializeField] private ActionList _FALSE;
        [SerializeField] private ActionListCommand _actionListFalseCommand;
        
        public void Validate(int index) { }
        public void Init() {}
        
        public async UniTask<bool> Run()
        {
            var result = false;
            CompareStatement compare;
            if (_command == ActionIfElseCommand.Greater)
                compare = new CompareGreater { op1 = var1, op2 = var2 };
            else if (_command == ActionIfElseCommand.Lower)
                compare = new CompareLower { op1 = var1, op2 = var2 };
            else if (_command == ActionIfElseCommand.Equal)
                compare = new CompareEqual { op1 = var1, op2 = var2 };
            else if (_command == ActionIfElseCommand.NotEqual)
                compare = new CompareNotEqual { op1 = var1, op2 = var2 };
            else if (_command == ActionIfElseCommand.GreaterOrEqual)
                compare = new CompareGreaterOrEqual() { op1 = var1, op2 = var2 };
            else if (_command == ActionIfElseCommand.LowerOrEqual)
                compare = new CompareLowerOrEqual() { op1 = var1, op2 = var2 };
            else
                return true;

            var isSuccess = false;
            
            result = compare.GetResult();
            if(result)
                isSuccess = await RunActionList(_TRUE, _actionListTrueCommand);
            else
                isSuccess = await RunActionList(_FALSE, _actionListFalseCommand);
            
            return isSuccess;
        }

        public async UniTask<bool> RunActionList(ActionList al, ActionListCommand command)
        {
            var isSuccess = false;
            switch (command)
            {
                case ActionListCommand.Run:
                    isSuccess = await al.Run();
                    break;
                case ActionListCommand.Continue:
                    al.ContinueManually();
                    break;
                case ActionListCommand.Stop:
                    al.Stop();
                    break;
                case ActionListCommand.RunAsync:
                    isSuccess = true;
                    al.RunManually();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return isSuccess;
        }
    }
    
    #region compare gates
    public abstract class CompareStatement
    {
        public bool GetResult() {  return Compare(op1.GetNumber(), op2.GetNumber()); }
        public VariableField op1;
        public VariableField op2;
        protected abstract bool Compare(double aOp1, double aOp2);
    }
    public class CompareEqual : CompareStatement
    {
        protected override bool Compare(double aOp1, double aOp2) { return aOp1 == aOp2; }
    }
    public class CompareNotEqual : CompareStatement
    {
        protected override bool Compare(double aOp1, double aOp2) { return aOp1 != aOp2; }
    }
    public class CompareGreater : CompareStatement
    {
        protected override bool Compare(double aOp1, double aOp2) { return aOp1 > aOp2; }
    }
    public class CompareGreaterOrEqual : CompareStatement
    {
        protected override bool Compare(double aOp1, double aOp2) { return aOp1 >= aOp2; }
    }
    public class CompareLower : CompareStatement
    {
        protected override bool Compare(double aOp1, double aOp2) { return aOp1 < aOp2; }
    }
    public class CompareLowerOrEqual : CompareStatement
    {
        protected override bool Compare(double aOp1, double aOp2) { return aOp1 <= aOp2; }
    }
    #endregion compare gates
}