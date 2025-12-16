using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class ActionMenuPathAttribute : TooltipAttribute
{
    public ActionMenuPathAttribute(string tooltip) : base(tooltip)
    {
    }
}