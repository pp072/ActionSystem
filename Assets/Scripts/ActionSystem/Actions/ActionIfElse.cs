using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace ActionSystem
{
    public enum CompareOperator
    {
        Greater,
        Less,
        Equal,
        NotEqual,
        GreaterOrEqual,
        LessOrEqual,
    }

    public enum CompareType
    {
        Int,
        Float,
        Bool,
        String
    }

    public enum BranchType
    {
        Continue,
        Stop,
        GoTo
    }

    [Serializable, ActionMenuPath("Logic/If Else", 0.8f, 0.6f, 0.2f)]
    public class ActionIfElse : ActionItemBase, IFlowControlAction
    {
        [SerializeField] private CompareType _compareType = CompareType.Int;

        [ShowIf(nameof(IsIntCompare)), AllowNesting]
        [SerializeField] private IntRef _intValue1;

        [ShowIf(nameof(IsIntCompare)), AllowNesting]
        [SerializeField] private CompareOperator _intOperator;

        [ShowIf(nameof(IsIntCompare)), AllowNesting]
        [SerializeField] private IntRef _intValue2;

        [ShowIf(nameof(IsFloatCompare)), AllowNesting]
        [SerializeField] private FloatRef _floatValue1;

        [ShowIf(nameof(IsFloatCompare)), AllowNesting]
        [SerializeField] private CompareOperator _floatOperator;

        [ShowIf(nameof(IsFloatCompare)), AllowNesting]
        [SerializeField] private FloatRef _floatValue2;

        [ShowIf(nameof(IsBoolCompare)), AllowNesting]
        [SerializeField] private BoolRef _boolValue1;

        [ShowIf(nameof(IsBoolCompare)), AllowNesting]
        [SerializeField] private BoolRef _boolValue2;

        [ShowIf(nameof(IsStringCompare)), AllowNesting]
        [SerializeField] private StringRef _stringValue1;

        [ShowIf(nameof(IsStringCompare)), AllowNesting]
        [SerializeField] private StringRef _stringValue2;

        [BoxGroup("If TRUE"), AllowNesting]
        [SerializeField] private BranchType _trueBranch = BranchType.Continue;

        [BoxGroup("If TRUE"), AllowNesting]
        [ShowIf(nameof(IsTrueGoTo))]
        [Dropdown(nameof(GetAllActionsInfo))]
        [SerializeField] private int _trueGoTo;

        [BoxGroup("If FALSE"), AllowNesting]
        [SerializeField] private BranchType _falseBranch = BranchType.Continue;

        [BoxGroup("If FALSE"), AllowNesting]
        [ShowIf(nameof(IsFalseGoTo))]
        [Dropdown(nameof(GetAllActionsInfo))]
        [SerializeField] private int _falseGoTo;

        private bool _lastResult;

        private bool IsIntCompare => _compareType == CompareType.Int;
        private bool IsFloatCompare => _compareType == CompareType.Float;
        private bool IsBoolCompare => _compareType == CompareType.Bool;
        private bool IsStringCompare => _compareType == CompareType.String;
        private bool IsTrueGoTo => _trueBranch == BranchType.GoTo;
        private bool IsFalseGoTo => _falseBranch == BranchType.GoTo;

        public List<ActionInfo> ActionNodesList { get; } = new();

        public DropdownList<int> GetAllActionsInfo()
        {
            var list = new DropdownList<int>();
            foreach (var info in ActionNodesList)
            {
                list.Add(info.name, info.index);
            }
            return list;
        }

        public override void Validate(int index)
        {
            base.Validate(index);

            string condition = _compareType switch
            {
                CompareType.Int => $"{_intValue1.GetDisplayName()} {GetOperatorSymbol(_intOperator)} {_intValue2.GetDisplayName()}",
                CompareType.Float => $"{_floatValue1.GetDisplayName()} {GetOperatorSymbol(_floatOperator)} {_floatValue2.GetDisplayName()}",
                CompareType.Bool => $"{_boolValue1.GetDisplayName()} == {_boolValue2.GetDisplayName()}",
                CompareType.String => $"{_stringValue1.GetDisplayName()} == {_stringValue2.GetDisplayName()}",
                _ => "?"
            };

            string trueResult = _trueBranch switch
            {
                BranchType.Continue => "→",
                BranchType.Stop => "Stop",
                BranchType.GoTo => $"→{_trueGoTo}",
                _ => "?"
            };

            string falseResult = _falseBranch switch
            {
                BranchType.Continue => "→",
                BranchType.Stop => "Stop",
                BranchType.GoTo => $"→{_falseGoTo}",
                _ => "?"
            };

            Name = $"If ({condition}) T:{trueResult} F:{falseResult}";
        }

        private string GetOperatorSymbol(CompareOperator op)
        {
            return op switch
            {
                CompareOperator.Greater => ">",
                CompareOperator.Less => "<",
                CompareOperator.Equal => "==",
                CompareOperator.NotEqual => "!=",
                CompareOperator.GreaterOrEqual => ">=",
                CompareOperator.LessOrEqual => "<=",
                _ => "?"
            };
        }

        public override async UniTask<bool> Run()
        {
            _lastResult = _compareType switch
            {
                CompareType.Int => CompareInt(),
                CompareType.Float => CompareFloat(),
                CompareType.Bool => CompareBool(),
                CompareType.String => CompareString(),
                _ => true
            };

            return true;
        }

        private bool CompareInt()
        {
            int a = _intValue1.GetValue(Context);
            int b = _intValue2.GetValue(Context);

            return _intOperator switch
            {
                CompareOperator.Greater => a > b,
                CompareOperator.Less => a < b,
                CompareOperator.Equal => a == b,
                CompareOperator.NotEqual => a != b,
                CompareOperator.GreaterOrEqual => a >= b,
                CompareOperator.LessOrEqual => a <= b,
                _ => true
            };
        }

        private bool CompareFloat()
        {
            float a = _floatValue1.GetValue(Context);
            float b = _floatValue2.GetValue(Context);

            return _floatOperator switch
            {
                CompareOperator.Greater => a > b,
                CompareOperator.Less => a < b,
                CompareOperator.Equal => Mathf.Approximately(a, b),
                CompareOperator.NotEqual => !Mathf.Approximately(a, b),
                CompareOperator.GreaterOrEqual => a >= b,
                CompareOperator.LessOrEqual => a <= b,
                _ => true
            };
        }

        private bool CompareBool()
        {
            return _boolValue1.GetValue(Context) == _boolValue2.GetValue(Context);
        }

        private bool CompareString()
        {
            return _stringValue1.GetValue(Context) == _stringValue2.GetValue(Context);
        }

        public int GetNextIndex()
        {
            var branch = _lastResult ? _trueBranch : _falseBranch;
            var goTo = _lastResult ? _trueGoTo : _falseGoTo;

            return branch switch
            {
                BranchType.Continue => (int)FlowControlResult.Continue,
                BranchType.Stop => (int)FlowControlResult.Stop,
                BranchType.GoTo => goTo,
                _ => (int)FlowControlResult.Continue
            };
        }
    }
}
