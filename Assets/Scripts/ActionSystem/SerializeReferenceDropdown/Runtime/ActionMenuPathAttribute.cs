using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class ActionMenuPathAttribute : Attribute
{
    public string Path { get; }
    public string Name { get; }
    public string MenuPath { get; }
    public Color? HeaderColor { get; }

    public ActionMenuPathAttribute(string path)
    {
        Path = path;
        HeaderColor = null;

        int lastSlash = path.LastIndexOf('/');
        if (lastSlash >= 0)
        {
            MenuPath = path.Substring(0, lastSlash);
            Name = path.Substring(lastSlash + 1);
        }
        else
        {
            MenuPath = "";
            Name = path;
        }
    }

    public ActionMenuPathAttribute(string path, float r, float g, float b, float a = 0.3f) : this(path)
    {
        HeaderColor = new Color(r, g, b, a);
    }
}