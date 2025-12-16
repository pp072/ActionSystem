using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class ActionNameAttribute : TooltipAttribute
{
    public ActionNameAttribute(string tooltip) : base(tooltip)
    {
    }
}