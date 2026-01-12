using UnityEditor;
using System;
using UnityEngine;

internal static class InspectorRepaintScheduler
{
    private static bool _queued;

    public static void Request()
    {
        if (_queued)
            return;

        _queued = true;
        EditorApplication.delayCall += RepaintOnce;
    }

    private static void RepaintOnce()
    {
        _queued = false;

        // Repaint only Inspector windows
        foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            if (window.GetType().Name == "InspectorWindow")
            {
                window.Repaint();
            }
        }
    }
}