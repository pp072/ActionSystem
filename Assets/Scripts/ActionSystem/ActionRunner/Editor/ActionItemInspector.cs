using System.Collections.Generic;
using System.Reflection;
using ActionSystem;
using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ActionList), true)]
public class ActionItemInspector : NaughtyInspector
{
    // Track action order to detect reordering
    private List<int> _previousActionHashes = new();
    private List<int> _previousGoToValues = new();

    // Arrow visibility toggle
    public static bool ShowGoToArrows = true;

    public override void OnInspectorGUI()
    {
        var listProp = serializedObject.FindProperty("Actions");

        // Clear pending arrows before drawing
        ActionSystem.Editor.ActionDrawer.ClearPendingArrows();

        // Capture state before drawing
        CaptureActionState(listProp);

        base.OnInspectorGUI();
        serializedObject.Update();

        // Check if order changed and update GoTo indices
        DetectAndFixReordering(listProp);

        // Draw expand/collapse buttons
        DrawExpandCollapseButtons(listProp);

        // Draw arrows in foreground (after all properties are drawn)
        if (Event.current.type == EventType.Repaint)
        {
            ActionSystem.Editor.ActionDrawer.DrawPendingArrows();
        }

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

            ShowGoToArrows = GUILayout.Toggle(ShowGoToArrows, "Arrows", "Button", GUILayout.Width(60));

            if (GUILayout.Button("Flow", GUILayout.Width(50)))
            {
                var window = EditorWindow.GetWindow<ActionSystem.Editor.ActionFlowWindow>("Action Flow");
                window.SetTarget(target as ActionList);
            }
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

    private void CaptureActionState(SerializedProperty listProp)
    {
        _previousActionHashes.Clear();
        _previousGoToValues.Clear();

        if (listProp == null) return;

        for (int i = 0; i < listProp.arraySize; i++)
        {
            var element = listProp.GetArrayElementAtIndex(i);
            var actionItemProp = element.FindPropertyRelative("_actionItem");
            var goToProp = element.FindPropertyRelative("GoTo");

            // Use managed reference ID as unique identifier for each action
            int hash = actionItemProp != null ? actionItemProp.managedReferenceId.GetHashCode() : i;
            _previousActionHashes.Add(hash);
            _previousGoToValues.Add(goToProp?.intValue ?? -1);
        }
    }

    private void DetectAndFixReordering(SerializedProperty listProp)
    {
        if (listProp == null || _previousActionHashes.Count == 0) return;
        if (listProp.arraySize != _previousActionHashes.Count) return; // Size changed, not just reorder

        // Get current hashes
        var currentHashes = new List<int>();
        for (int i = 0; i < listProp.arraySize; i++)
        {
            var element = listProp.GetArrayElementAtIndex(i);
            var actionItemProp = element.FindPropertyRelative("_actionItem");
            int hash = actionItemProp != null ? actionItemProp.managedReferenceId.GetHashCode() : i;
            currentHashes.Add(hash);
        }

        // Check if order changed
        bool orderChanged = false;
        for (int i = 0; i < currentHashes.Count; i++)
        {
            if (currentHashes[i] != _previousActionHashes[i])
            {
                orderChanged = true;
                break;
            }
        }

        if (!orderChanged) return;

        // Build mapping: old index -> new index
        var oldToNewIndex = new Dictionary<int, int>();
        for (int oldIdx = 0; oldIdx < _previousActionHashes.Count; oldIdx++)
        {
            int hash = _previousActionHashes[oldIdx];
            for (int newIdx = 0; newIdx < currentHashes.Count; newIdx++)
            {
                if (currentHashes[newIdx] == hash)
                {
                    oldToNewIndex[oldIdx] = newIdx;
                    break;
                }
            }
        }

        // Update GoTo values based on the mapping
        bool changed = false;
        for (int i = 0; i < listProp.arraySize; i++)
        {
            var element = listProp.GetArrayElementAtIndex(i);
            var advancedRunTypeProp = element.FindPropertyRelative("AdvancedRunType");
            var runTypeProp = element.FindPropertyRelative("RunType");
            var finishTypeProp = element.FindPropertyRelative("FinishType");
            var goToProp = element.FindPropertyRelative("GoTo");
            var actionItemProp = element.FindPropertyRelative("_actionItem");

            bool isAdvanced = advancedRunTypeProp != null && advancedRunTypeProp.boolValue;
            bool isWait = runTypeProp != null && runTypeProp.enumValueIndex == 0;
            bool isGoTo = finishTypeProp != null && finishTypeProp.enumValueIndex == 3;

            // Update regular GoTo
            if (isAdvanced && isWait && isGoTo && goToProp != null)
            {
                int currentGoTo = goToProp.intValue;
                if (oldToNewIndex.TryGetValue(currentGoTo, out int newGoTo) && newGoTo != currentGoTo)
                {
                    goToProp.intValue = newGoTo;
                    changed = true;
                }
            }

            // Update If/Else GoTo values
            if (actionItemProp != null && actionItemProp.managedReferenceValue is ActionIfElse)
            {
                var trueGoToProp = actionItemProp.FindPropertyRelative("_trueGoTo");
                var falseGoToProp = actionItemProp.FindPropertyRelative("_falseGoTo");
                var trueBranchProp = actionItemProp.FindPropertyRelative("_trueBranch");
                var falseBranchProp = actionItemProp.FindPropertyRelative("_falseBranch");

                // Update true GoTo if it's a GoTo branch (GoTo = 2 in BranchType enum)
                if (trueBranchProp != null && trueBranchProp.enumValueIndex == 2 && trueGoToProp != null)
                {
                    int currentTrueGoTo = trueGoToProp.intValue;
                    if (oldToNewIndex.TryGetValue(currentTrueGoTo, out int newTrueGoTo) && newTrueGoTo != currentTrueGoTo)
                    {
                        trueGoToProp.intValue = newTrueGoTo;
                        changed = true;
                    }
                }

                // Update false GoTo if it's a GoTo branch (GoTo = 2 in BranchType enum)
                if (falseBranchProp != null && falseBranchProp.enumValueIndex == 2 && falseGoToProp != null)
                {
                    int currentFalseGoTo = falseGoToProp.intValue;
                    if (oldToNewIndex.TryGetValue(currentFalseGoTo, out int newFalseGoTo) && newFalseGoTo != currentFalseGoTo)
                    {
                        falseGoToProp.intValue = newFalseGoTo;
                        changed = true;
                    }
                }
            }
        }

        if (changed)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}