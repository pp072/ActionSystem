using ActionSystem;
using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ActionList), true)]
public class ActionItemInspector : NaughtyInspector
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        var listProp = serializedObject.FindProperty("Actions");

        // Draw expand/collapse buttons
        DrawExpandCollapseButtons(listProp);


        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawExpandCollapseButtons(SerializedProperty listProp)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Expand All"))
                SetListExpanded(listProp, true);

            if (GUILayout.Button("Collapse All"))
                SetListExpanded(listProp, false);
        }
    }

    private void SetListExpanded(SerializedProperty listProp, bool expanded)
    {
        for (int i = 0; i < listProp.arraySize; i++)
        {
            var element = listProp.GetArrayElementAtIndex(i);
            SetExpandedRecursive(element, expanded);
        }
    }

    private void SetExpandedRecursive(SerializedProperty property, bool expanded)
    {
        property.isExpanded = expanded;

        var child = property.Copy();
        var end = child.GetEndProperty();

        child.NextVisible(true);
        while (!SerializedProperty.EqualContents(child, end))
        {
            child.isExpanded = expanded;
            child.NextVisible(false);
        }
    }
}