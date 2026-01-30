using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionSystem.Editor
{
    [CustomPropertyDrawer(typeof(Action))]
    public class ActionDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 20f;
        private const float ButtonSpacing = 2f;
        private const float ArrowWidth = 20f;
        private const float RightMargin = 40f;

        private static int s_DraggedIndex = -1;
        private static int s_DropTargetIndex = -1;
        private static bool s_IsDragging = false;
        private static bool s_DraggedWasExpanded = false;
        private static SerializedProperty s_DraggedArrayProp;

        // For GoTo arrows
        private static Dictionary<int, Rect> s_ActionRects = new();

        // Deferred arrow drawing (to render in foreground)
        public struct ArrowDrawRequest
        {
            public int FromIndex;
            public int ToIndex;
            public Color Color;
            public int OffsetSlot;
        }
        public static List<ArrowDrawRequest> PendingArrows = new();

        public static void DrawPendingArrows()
        {
            foreach (var arrow in PendingArrows)
            {
                if (s_ActionRects.ContainsKey(arrow.FromIndex) && s_ActionRects.ContainsKey(arrow.ToIndex))
                {
                    DrawSingleArrow(arrow.FromIndex, arrow.ToIndex, arrow.Color, arrow.OffsetSlot);
                }
            }
            PendingArrows.Clear();
        }

        public static void ClearPendingArrows()
        {
            PendingArrows.Clear();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;

            // Calculate button positions on the right (4 buttons: up, down, duplicate, delete)
            float buttonsWidth = (ButtonWidth * 4) + (ButtonSpacing * 3);

            // Header rect (first line only, narrowed for buttons and margin)
            Rect headerRect = new Rect(position.x, position.y, position.width - buttonsWidth - RightMargin - 4f, lineHeight);

            float buttonX = position.xMax - buttonsWidth - RightMargin;
            Rect upButtonRect = new Rect(buttonX, position.y, ButtonWidth, lineHeight);
            buttonX += ButtonWidth + ButtonSpacing;
            Rect downButtonRect = new Rect(buttonX, position.y, ButtonWidth, lineHeight);
            buttonX += ButtonWidth + ButtonSpacing;
            Rect duplicateButtonRect = new Rect(buttonX, position.y, ButtonWidth, lineHeight);
            buttonX += ButtonWidth + ButtonSpacing;
            Rect deleteButtonRect = new Rect(buttonX, position.y, ButtonWidth, lineHeight);

            var arrayProp = GetParentArrayProperty(property);
            int index = GetArrayIndex(property);
            int arraySize = arrayProp?.arraySize ?? 0;

            // Handle drag and drop by header
            HandleDragAndDrop(headerRect, position, property, arrayProp, index);

            // Handle right-click context menu
            HandleContextMenu(position, property);

            // Check if action is skipped and get run type properties
            var advancedRunTypeProp = property.FindPropertyRelative("AdvancedRunType");
            var runTypeProp = property.FindPropertyRelative("RunType");
            var finishTypeProp = property.FindPropertyRelative("FinishType");
            var goToProp = property.FindPropertyRelative("GoTo");
            var actionItemProp = property.FindPropertyRelative("_actionItem");

            bool isSkip = runTypeProp != null && runTypeProp.enumValueIndex == 2; // Skip = 2

            // Check if action has GoTo (must have AdvancedRunType=true, RunType=Wait, FinishType=GoTo)
            bool isAdvanced = advancedRunTypeProp != null && advancedRunTypeProp.boolValue;
            bool isWait = runTypeProp != null && runTypeProp.enumValueIndex == 0; // Wait = 0
            bool isGoToFinish = finishTypeProp != null && finishTypeProp.enumValueIndex == 3; // GoTo = 3
            bool isGoTo = isAdvanced && isWait && isGoToFinish;
            int goToTarget = goToProp?.intValue ?? -1;

            // Update rect and queue arrow drawing during Repaint
            if (Event.current.type == EventType.Repaint)
            {
                // Update rect for this element
                s_ActionRects[index] = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

                // Queue arrow if enabled and this element has GoTo and target rect exists
                if (ActionItemInspector.ShowGoToArrows && isGoTo && goToTarget >= 0 && goToTarget < arraySize)
                {
                    PendingArrows.Add(new ArrowDrawRequest
                    {
                        FromIndex = index,
                        ToIndex = goToTarget,
                        Color = new Color(0.3f, 0.6f, 1f, 0.8f),
                        OffsetSlot = -1
                    });
                }

                // Queue If/Else arrows
                if (ActionItemInspector.ShowGoToArrows && actionItemProp != null && actionItemProp.managedReferenceValue is ActionIfElse ifElse)
                {
                    QueueIfElseArrows(index, ifElse, arraySize);
                }
            }

            // Draw header background based on state
            Rect bgRect = new Rect(position.x - 2, position.y, position.width + 4, EditorGUIUtility.singleLineHeight);

            // Check if action is in progress
            bool isInProgress = false;
            var targetObject = property.serializedObject.targetObject;
            if (targetObject is ActionList actionList && index >= 0)
            {
                var actions = actionList.ActionsList;
                if (actions != null && index < actions.Count)
                {
                    isInProgress = actions[index].IsInProgress;
                }
            }

            // Get custom color from ActionMenuPathAttribute if set
            Color? customColor = null;
            if (actionItemProp != null && actionItemProp.managedReferenceValue != null)
            {
                var type = actionItemProp.managedReferenceValue.GetType();
                var attrs = type.GetCustomAttributes(typeof(ActionMenuPathAttribute), false);
                if (attrs.Length > 0)
                {
                    customColor = ((ActionMenuPathAttribute)attrs[0]).HeaderColor;
                }
            }

            if (isInProgress)
            {
                EditorGUI.DrawRect(bgRect, new Color(0.2f, 0.8f, 0.2f, 0.3f));
            }
            else if (isSkip)
            {
                EditorGUI.DrawRect(bgRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            }
            else if (customColor.HasValue)
            {
                EditorGUI.DrawRect(bgRect, customColor.Value);
            }
            else if (isGoTo)
            {
                EditorGUI.DrawRect(bgRect, new Color(0.4f, 0.7f, 1f, 0.25f));
            }

            // Draw red square on right side for Stop state
            bool isStop = isAdvanced && isWait && finishTypeProp != null && finishTypeProp.enumValueIndex == 1;
            if (isStop)
            {
                float squareSize = 4f;
                Rect stopRect = new Rect(bgRect.xMax - squareSize - 6f, bgRect.y + (lineHeight - squareSize) / 2f, squareSize, squareSize);
                EditorGUI.DrawRect(stopRect, new Color(1f, 0.3f, 0.3f, 0.8f));
            }

            // Build display name dynamically
            string displayName = BuildDisplayName(property, index, isSkip);

            // Draw foldout on header line (grayed out if skipped)
            using (new EditorGUI.DisabledScope(isSkip))
            {
                property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, displayName, true);
            }

            // Draw expanded content at full width
            if (property.isExpanded)
            {
                Rect contentRect = new Rect(position.x, position.y + lineHeight + 2f, position.width, position.height - lineHeight - 2f);

                EditorGUI.indentLevel++;
                var childProp = property.Copy();
                var endProp = childProp.GetEndProperty();
                childProp.NextVisible(true);

                float yOffset = 0;
                while (!SerializedProperty.EqualContents(childProp, endProp))
                {
                    float propHeight = EditorGUI.GetPropertyHeight(childProp, true);
                    Rect propRect = new Rect(contentRect.x, contentRect.y + yOffset, contentRect.width, propHeight);
                    EditorGUI.PropertyField(propRect, childProp, true);
                    yOffset += propHeight + 2f;

                    if (!childProp.NextVisible(false))
                        break;
                }
                EditorGUI.indentLevel--;
            }

            // Draw up button (disabled if first element)
            EditorGUI.BeginDisabledGroup(index <= 0);
            if (GUI.Button(upButtonRect, "▲"))
            {
                MoveArrayElementWithExpandedState(arrayProp, index, index - 1);
                property.serializedObject.ApplyModifiedProperties();
                TriggerValidation(property);
            }
            EditorGUI.EndDisabledGroup();

            // Draw down button (disabled if last element)
            EditorGUI.BeginDisabledGroup(index >= arraySize - 1);
            if (GUI.Button(downButtonRect, "▼"))
            {
                MoveArrayElementWithExpandedState(arrayProp, index, index + 1);
                property.serializedObject.ApplyModifiedProperties();
                TriggerValidation(property);
            }
            EditorGUI.EndDisabledGroup();

            // Draw duplicate button
            if (GUI.Button(duplicateButtonRect, "+"))
            {
                arrayProp?.InsertArrayElementAtIndex(index);
                property.serializedObject.ApplyModifiedProperties();
                TriggerValidation(property);
            }

            // Draw delete button
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUI.Button(deleteButtonRect, "×"))
            {
                arrayProp?.DeleteArrayElementAtIndex(index);
                property.serializedObject.ApplyModifiedProperties();
                TriggerValidation(property);
            }
            GUI.backgroundColor = oldColor;


            // Force continuous repaint during any drag (standard or custom)
            if (GUIUtility.hotControl != 0 || s_IsDragging)
            {
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            EditorGUI.EndProperty();
        }

        private static void DrawSingleArrow(int fromIndex, int toIndex, Color arrowColor, int offsetSlot = -1)
        {
            // Get current rect for this element (source)
            if (!s_ActionRects.TryGetValue(fromIndex, out var fromRect)) return;
            if (!s_ActionRects.TryGetValue(toIndex, out var toRect)) return;

            // During custom drag, adjust visual positions
            int visualFrom = fromIndex;
            int visualTo = toIndex;
            if (s_IsDragging && s_DraggedIndex >= 0 && s_DropTargetIndex >= 0 && s_DropTargetIndex != s_DraggedIndex)
            {
                visualFrom = GetPreviewIndex(fromIndex, s_DraggedIndex, s_DropTargetIndex);
                visualTo = GetPreviewIndex(toIndex, s_DraggedIndex, s_DropTargetIndex);

                // Get rects for preview positions
                if (!s_ActionRects.TryGetValue(visualFrom, out fromRect)) return;
                if (!s_ActionRects.TryGetValue(visualTo, out toRect)) return;
            }

            // Offset vertical line based on source index or custom slot to prevent overlap
            int slot = offsetSlot >= 0 ? offsetSlot : fromIndex;
            float verticalOffset = (slot % 5) * 5f;

            // Draw arrow on the right side (in the margin area)
            float xStart = fromRect.xMax - RightMargin + 4;
            float xVertical = fromRect.xMax - 6 - verticalOffset;
            float fromY = fromRect.y + fromRect.height / 2;
            float toY = toRect.y + toRect.height / 2;

            // Vertical line (shifted based on index)
            float minY = Mathf.Min(fromY, toY);
            float maxY = Mathf.Max(fromY, toY);
            EditorGUI.DrawRect(new Rect(xVertical, minY, 2, maxY - minY), arrowColor);

            // Horizontal line from source (connects element to vertical line)
            EditorGUI.DrawRect(new Rect(xStart, fromY - 1, xVertical - xStart + 2, 2), arrowColor);

            // Horizontal line to target (connects vertical line to arrow head)
            float arrowHeadX = xStart + 6;
            EditorGUI.DrawRect(new Rect(arrowHeadX, toY - 1, xVertical - arrowHeadX + 2, 2), arrowColor);

            // Arrow head pointing left (toward the target element)
            DrawArrowHead(arrowHeadX, toY, arrowColor);
        }

        private static void QueueIfElseArrows(int index, ActionIfElse ifElse, int arraySize)
        {
            var type = typeof(ActionIfElse);
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

            var trueBranch = (BranchType)type.GetField("_trueBranch", bindingFlags).GetValue(ifElse);
            var trueGoTo = (int)type.GetField("_trueGoTo", bindingFlags).GetValue(ifElse);
            var falseBranch = (BranchType)type.GetField("_falseBranch", bindingFlags).GetValue(ifElse);
            var falseGoTo = (int)type.GetField("_falseGoTo", bindingFlags).GetValue(ifElse);

            // Queue true GoTo arrow (green)
            if (trueBranch == BranchType.GoTo && trueGoTo >= 0 && trueGoTo < arraySize)
            {
                PendingArrows.Add(new ArrowDrawRequest
                {
                    FromIndex = index,
                    ToIndex = trueGoTo,
                    Color = new Color(0.3f, 0.8f, 0.3f, 0.8f),
                    OffsetSlot = index * 2
                });
            }

            // Queue false GoTo arrow (red)
            if (falseBranch == BranchType.GoTo && falseGoTo >= 0 && falseGoTo < arraySize)
            {
                PendingArrows.Add(new ArrowDrawRequest
                {
                    FromIndex = index,
                    ToIndex = falseGoTo,
                    Color = new Color(1f, 0.3f, 0.3f, 0.8f),
                    OffsetSlot = index * 2 + 1
                });
            }
        }

        private static void DrawArrowHead(float x, float y, Color color)
        {
            // Bigger arrow head pointing left
            float size = 8f;
            for (int i = 0; i < 5; i++)
            {
                float offset = i * 2f;
                float height = size - i * 1.6f;
                EditorGUI.DrawRect(new Rect(x - offset, y - height / 2, 2, height), color);
            }
        }

        private static int GetPreviewIndex(int currentIndex, int fromIndex, int toIndex)
        {
            // The moved element goes to toIndex
            if (currentIndex == fromIndex)
            {
                return toIndex;
            }

            // Elements shift due to the move
            if (fromIndex < toIndex)
            {
                // Moving down: elements between from and to shift up by 1
                if (currentIndex > fromIndex && currentIndex <= toIndex)
                {
                    return currentIndex - 1;
                }
            }
            else
            {
                // Moving up: elements between to and from shift down by 1
                if (currentIndex >= toIndex && currentIndex < fromIndex)
                {
                    return currentIndex + 1;
                }
            }

            return currentIndex;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                var childProp = property.Copy();
                var endProp = childProp.GetEndProperty();
                childProp.NextVisible(true);

                while (!SerializedProperty.EqualContents(childProp, endProp))
                {
                    height += EditorGUI.GetPropertyHeight(childProp, true) + 2f;
                    if (!childProp.NextVisible(false))
                        break;
                }
                height += 4f;
            }

            return height;
        }

        private SerializedProperty GetParentArrayProperty(SerializedProperty property)
        {
            string path = property.propertyPath;
            int lastBracket = path.LastIndexOf('[');
            if (lastBracket < 0) return null;

            string arrayPath = path.Substring(0, lastBracket - ".Array.data".Length);
            return property.serializedObject.FindProperty(arrayPath);
        }

        private int GetArrayIndex(SerializedProperty property)
        {
            string path = property.propertyPath;
            int start = path.LastIndexOf('[') + 1;
            int end = path.LastIndexOf(']');
            if (start < 0 || end < 0) return -1;

            string indexStr = path.Substring(start, end - start);
            return int.TryParse(indexStr, out int index) ? index : -1;
        }

        private void TriggerValidation(SerializedProperty property)
        {
            // Trigger OnValidate to update action names/indices
            var targetObject = property.serializedObject.targetObject;
            if (targetObject is MonoBehaviour mb)
            {
                EditorUtility.SetDirty(mb);
            }
        }

        private void MoveArrayElementWithExpandedState(SerializedProperty arrayProp, int fromIndex, int toIndex)
        {
            if (arrayProp == null) return;

            int arraySize = arrayProp.arraySize;

            // Save expanded states BEFORE move
            bool[] expandedStates = new bool[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                expandedStates[i] = arrayProp.GetArrayElementAtIndex(i).isExpanded;
            }

            // Perform the move
            arrayProp.MoveArrayElement(fromIndex, toIndex);

            // Restore expanded states AFTER move
            for (int i = 0; i < arraySize; i++)
            {
                int originalIndex = GetOriginalIndex(i, fromIndex, toIndex);
                arrayProp.GetArrayElementAtIndex(i).isExpanded = expandedStates[originalIndex];
            }

            // GoTo indices are updated by ActionItemInspector.DetectAndFixReordering
        }

        private int GetOriginalIndex(int currentIndex, int fromIndex, int toIndex)
        {
            if (currentIndex == toIndex)
            {
                return fromIndex;
            }
            else if (fromIndex < toIndex && currentIndex >= fromIndex && currentIndex < toIndex)
            {
                return currentIndex + 1;
            }
            else if (fromIndex > toIndex && currentIndex > toIndex && currentIndex <= fromIndex)
            {
                return currentIndex - 1;
            }
            return currentIndex;
        }

        private string BuildDisplayName(SerializedProperty property, int index, bool isSkip)
        {
            // Get action item and its name
            var actionItemProp = property.FindPropertyRelative("_actionItem");
            string actionName = "Empty";

            if (actionItemProp != null && actionItemProp.managedReferenceValue != null)
            {
                var actionItem = actionItemProp.managedReferenceValue as IActionItem;
                if (actionItem != null && !string.IsNullOrEmpty(actionItem.Name))
                {
                    actionName = actionItem.Name;
                }
                else
                {
                    // Get name from ActionMenuPathAttribute
                    var type = actionItemProp.managedReferenceValue.GetType();
                    var attr = type.GetCustomAttributes(typeof(ActionMenuPathAttribute), false);
                    if (attr.Length > 0)
                        actionName = ((ActionMenuPathAttribute)attr[0]).Name;
                    else
                        actionName = type.Name;
                }
            }

            // Check FinishType for GoTo
            var finishTypeProp = property.FindPropertyRelative("FinishType");
            var goToProp = property.FindPropertyRelative("GoTo");
            string goToText = "";

            if (finishTypeProp != null && finishTypeProp.enumValueIndex == 3 && goToProp != null) // GoTo = 3
            {
                goToText = $" -> Go to {goToProp.intValue}";
            }

            return $"{index}: {actionName}{(isSkip ? "  [SKIP]" : "")}{goToText}";
        }

        private void HandleDragAndDrop(Rect headerRect, Rect fullRect, SerializedProperty property, SerializedProperty arrayProp, int index)
        {
            if (arrayProp == null) return;

            Event evt = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive, headerRect);

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (headerRect.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        s_DraggedIndex = index;
                        s_DropTargetIndex = -1;
                        s_IsDragging = false;
                        s_DraggedWasExpanded = property.isExpanded;
                        s_DraggedArrayProp = arrayProp;
                        GUIUtility.hotControl = id;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && s_DraggedIndex >= 0)
                    {
                        GUIUtility.hotControl = 0;

                        // Only move if we actually dragged and have a valid drop target
                        if (s_IsDragging && s_DropTargetIndex >= 0 && s_DropTargetIndex != s_DraggedIndex)
                        {
                            MoveArrayElementWithExpandedState(arrayProp, s_DraggedIndex, s_DropTargetIndex);
                            property.serializedObject.ApplyModifiedProperties();
                            TriggerValidation(property);
                        }

                        s_DraggedIndex = -1;
                        s_DropTargetIndex = -1;
                        s_IsDragging = false;
                        s_DraggedWasExpanded = false;
                        s_DraggedArrayProp = null;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_IsDragging = true;
                        evt.Use();
                    }
                    // Track drop target during drag - check if mouse is over this element
                    if (s_IsDragging && s_DraggedIndex >= 0 && s_DraggedIndex != index)
                    {
                        if (fullRect.Contains(evt.mousePosition))
                        {
                            s_DropTargetIndex = index;
                        }
                    }
                    break;

                case EventType.Repaint:
                    // Track drop target during repaint as well for smooth updates
                    if (s_IsDragging && s_DraggedIndex >= 0 && s_DraggedIndex != index)
                    {
                        if (fullRect.Contains(evt.mousePosition))
                        {
                            s_DropTargetIndex = index;
                        }
                    }

                    // Draw drop indicator
                    if (s_IsDragging && s_DraggedIndex >= 0 && s_DraggedArrayProp != null &&
                        SerializedProperty.EqualContents(s_DraggedArrayProp, arrayProp) &&
                        s_DropTargetIndex == index)
                    {
                        // Show above when dragging up, below when dragging down
                        float indicatorY = s_DraggedIndex > index ? fullRect.y - 2 : fullRect.yMax + 1;
                        Rect indicatorRect = new Rect(fullRect.x, indicatorY, fullRect.width, 2);
                        EditorGUI.DrawRect(indicatorRect, new Color(0.2f, 0.6f, 1f, 1f));
                    }
                    break;
            }
        }

        private void HandleContextMenu(Rect position, SerializedProperty property)
        {
            Event evt = Event.current;

            // Ctrl+Click to open script
            if (evt.type == EventType.MouseDown && evt.button == 0 && evt.control)
            {
                if (position.Contains(evt.mousePosition))
                {
                    var actionItemProp = property.FindPropertyRelative("_actionItem");
                    if (actionItemProp != null && actionItemProp.managedReferenceValue != null)
                    {
                        OpenScriptFile(actionItemProp.managedReferenceValue.GetType());
                        evt.Use();
                    }
                }
            }
        }

        private void OpenScriptFile(Type type)
        {
            // Find script by type name
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                {
                    AssetDatabase.OpenAsset(script);
                    return;
                }
            }

            // Fallback: try to find by filename
            string[] allGuids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
            if (allGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(allGuids[0]);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null)
                {
                    AssetDatabase.OpenAsset(script);
                }
            }
        }
    }
}
