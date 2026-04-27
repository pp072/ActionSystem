using System;

namespace ActionSystem
{
    [AttributeUsage(AttributeTargets.Field)] public class ShowIfAttribute : Attribute { public string Condition; public ShowIfAttribute(string c) => Condition = c; }
    [AttributeUsage(AttributeTargets.Field)] public class HideIfAttribute : Attribute { public string Condition; public HideIfAttribute(string c) => Condition = c; }
    [AttributeUsage(AttributeTargets.Field)] public class BoxGroupAttribute : Attribute { public string Name; public BoxGroupAttribute(string n) => Name = n; }
    [AttributeUsage(AttributeTargets.Field)] public class DropdownAttribute : Attribute { public string Provider; public DropdownAttribute(string p) => Provider = p; }

    public struct ActionInfo
    {
        public int index;
        public string name;
    }
}
