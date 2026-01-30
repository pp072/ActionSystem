using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ActionSystem;

namespace ActionSystem.Editor
{
    public class ActionFlowWindow : EditorWindow
    {
        private ActionList _target;
        private Vector2 _scrollPosition;
        private Vector2 _drag;
        private Vector2 _offset;
        private float _zoom = 1f;

        private const float NodeWidth = 180f;
        private const float NodeHeight = 40f;
        private const float NodeSpacingX = 200f;
        private const float NodeSpacingY = 70f;
        private const float ConnectionWidth = 2f;

        private List<Rect> _nodeRects = new();
        private Dictionary<int, Vector2> _nodePositions = new();
        private Dictionary<int, Vector2> _nodeDragOffsets = new();
        private int _draggedNodeIndex = -1;
        private bool _isDraggingNode = false;

        [MenuItem("Window/Action System/Flow Visualizer")]
        public static void ShowWindow()
        {
            GetWindow<ActionFlowWindow>("Action Flow");
        }

        public void SetTarget(ActionList target)
        {
            _target = target;
            _nodeDragOffsets.Clear();
            Repaint();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeGameObject != null)
            {
                var actionList = Selection.activeGameObject.GetComponent<ActionList>();
                if (actionList != null)
                {
                    _target = actionList;
                    Repaint();
                }
            }
        }

        private void OnGUI()
        {
            if (_target == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject with ActionList component", MessageType.Info);
                return;
            }

            DrawToolbar();
            HandleInput();
            DrawGrid();
            DrawNodes();
            DrawConnections();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(_target.name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Reset View", EditorStyles.toolbarButton))
                {
                    _offset = Vector2.zero;
                    _zoom = 1f;
                }

                if (GUILayout.Button("Reset Layout", EditorStyles.toolbarButton))
                {
                    _nodeDragOffsets.Clear();
                }

                GUILayout.Label($"Zoom: {_zoom:F1}x", EditorStyles.toolbarButton);
            }
        }

        private void HandleInput()
        {
            Event e = Event.current;

            // Node dragging - mouse down
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                for (int i = 0; i < _nodeRects.Count; i++)
                {
                    if (_nodeRects[i].Contains(e.mousePosition))
                    {
                        _draggedNodeIndex = i;
                        _isDraggingNode = true;
                        e.Use();
                        break;
                    }
                }
            }

            // Node dragging - mouse drag
            if (e.type == EventType.MouseDrag && _isDraggingNode && _draggedNodeIndex >= 0)
            {
                Vector2 delta = e.delta / _zoom;
                if (!_nodeDragOffsets.ContainsKey(_draggedNodeIndex))
                    _nodeDragOffsets[_draggedNodeIndex] = Vector2.zero;
                _nodeDragOffsets[_draggedNodeIndex] += delta;
                e.Use();
                Repaint();
            }

            // Node dragging - mouse up
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _isDraggingNode = false;
                _draggedNodeIndex = -1;
            }

            // Pan with middle mouse or alt+left mouse
            if (e.type == EventType.MouseDrag)
            {
                if (e.button == 2 || (e.button == 0 && e.alt))
                {
                    _offset += e.delta;
                    e.Use();
                    Repaint();
                }
            }

            // Zoom with scroll wheel
            if (e.type == EventType.ScrollWheel)
            {
                float zoomDelta = -e.delta.y * 0.05f;
                _zoom = Mathf.Clamp(_zoom + zoomDelta, 0.5f, 2f);
                e.Use();
                Repaint();
            }
        }

        private void DrawGrid()
        {
            float gridSize = 20f * _zoom;
            int widthDivs = Mathf.CeilToInt(position.width / gridSize);
            int heightDivs = Mathf.CeilToInt(position.height / gridSize);

            Handles.BeginGUI();
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);

            Vector2 offset = new Vector2(_offset.x % gridSize, _offset.y % gridSize);

            for (int i = 0; i <= widthDivs; i++)
            {
                Handles.DrawLine(
                    new Vector3(gridSize * i + offset.x, 0, 0),
                    new Vector3(gridSize * i + offset.x, position.height, 0));
            }

            for (int j = 0; j <= heightDivs; j++)
            {
                Handles.DrawLine(
                    new Vector3(0, gridSize * j + offset.y, 0),
                    new Vector3(position.width, gridSize * j + offset.y, 0));
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawNodes()
        {
            _nodeRects.Clear();
            _nodePositions.Clear();

            var serializedObject = new SerializedObject(_target);
            var actionsProp = serializedObject.FindProperty("Actions");

            if (actionsProp == null) return;

            // First pass: calculate tree positions
            CalculateTreeLayout(actionsProp);

            // Second pass: draw nodes
            for (int i = 0; i < actionsProp.arraySize; i++)
            {
                var actionProp = actionsProp.GetArrayElementAtIndex(i);
                var actionItemProp = actionProp.FindPropertyRelative("_actionItem");
                var runTypeProp = actionProp.FindPropertyRelative("RunType");
                var advancedRunTypeProp = actionProp.FindPropertyRelative("AdvancedRunType");
                var finishTypeProp = actionProp.FindPropertyRelative("FinishType");

                bool isSkip = runTypeProp != null && runTypeProp.enumValueIndex == 2;
                bool isAdvanced = advancedRunTypeProp != null && advancedRunTypeProp.boolValue;
                bool isWait = runTypeProp != null && runTypeProp.enumValueIndex == 0;
                bool isGoTo = isAdvanced && isWait && finishTypeProp != null && finishTypeProp.enumValueIndex == 3;
                bool isStop = isAdvanced && isWait && finishTypeProp != null && finishTypeProp.enumValueIndex == 1;
                bool isInProgress = _target.ActionsList != null && i < _target.ActionsList.Count && _target.ActionsList[i].IsInProgress;

                // Get action name
                string actionName = "Empty";
                Color? customColor = null;

                if (actionItemProp != null && actionItemProp.managedReferenceValue != null)
                {
                    var actionItem = actionItemProp.managedReferenceValue as IActionItem;
                    if (actionItem != null && !string.IsNullOrEmpty(actionItem.Name))
                    {
                        actionName = actionItem.Name;
                    }
                    else
                    {
                        var type = actionItemProp.managedReferenceValue.GetType();
                        var attrs = type.GetCustomAttributes(typeof(ActionMenuPathAttribute), false);
                        if (attrs.Length > 0)
                        {
                            var attr = (ActionMenuPathAttribute)attrs[0];
                            actionName = attr.Name;
                            customColor = attr.HeaderColor;
                        }
                        else
                        {
                            actionName = type.Name;
                        }
                    }
                }

                // Get calculated position (center horizontally in window)
                Vector2 pos = _nodePositions.ContainsKey(i) ? _nodePositions[i] : new Vector2(0, i * NodeSpacingY);

                // Apply drag offset if exists
                if (_nodeDragOffsets.ContainsKey(i))
                    pos += _nodeDragOffsets[i];

                float centerX = position.width * 0.5f;
                float nodeX = centerX + _offset.x + pos.x * _zoom - (NodeWidth * _zoom * 0.5f);
                float nodeY = 60f + _offset.y + pos.y * _zoom;
                Rect nodeRect = new Rect(nodeX, nodeY, NodeWidth * _zoom, NodeHeight * _zoom);
                _nodeRects.Add(nodeRect);

                // Draw node
                DrawNode(nodeRect, i, actionName, isSkip, isGoTo, isStop, isInProgress, customColor);
            }

            serializedObject.Dispose();
        }

        private void CalculateTreeLayout(SerializedProperty actionsProp)
        {
            if (actionsProp.arraySize == 0) return;

            // Simple layout: all nodes in vertical column, only GoTo targets offset
            var goToTargets = new Dictionary<int, (float x, float y)>(); // index -> position

            // First pass: find all GoTo targets from If/Else and mark their offsets
            for (int i = 0; i < actionsProp.arraySize; i++)
            {
                var actionProp = actionsProp.GetArrayElementAtIndex(i);
                var actionItemProp = actionProp.FindPropertyRelative("_actionItem");

                if (actionItemProp != null && actionItemProp.managedReferenceValue is ActionIfElse ifElse)
                {
                    var type = typeof(ActionIfElse);
                    var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

                    var trueBranch = (BranchType)type.GetField("_trueBranch", bindingFlags).GetValue(ifElse);
                    var trueGoTo = (int)type.GetField("_trueGoTo", bindingFlags).GetValue(ifElse);
                    var falseBranch = (BranchType)type.GetField("_falseBranch", bindingFlags).GetValue(ifElse);
                    var falseGoTo = (int)type.GetField("_falseGoTo", bindingFlags).GetValue(ifElse);

                    // GoTo targets positioned parallel (same Y level, just below If/Else)
                    float branchY = (i + 1) * NodeSpacingY;

                    if (trueBranch == BranchType.GoTo && trueGoTo >= 0 && trueGoTo < actionsProp.arraySize)
                    {
                        if (!goToTargets.ContainsKey(trueGoTo))
                            goToTargets[trueGoTo] = (-NodeSpacingX * 0.6f, branchY); // Left
                    }
                    if (falseBranch == BranchType.GoTo && falseGoTo >= 0 && falseGoTo < actionsProp.arraySize)
                    {
                        if (!goToTargets.ContainsKey(falseGoTo))
                            goToTargets[falseGoTo] = (NodeSpacingX * 0.6f, branchY); // Right
                    }
                }
            }

            // Second pass: assign positions - vertical column with GoTo offsets
            float currentY = 0;
            var usedYPositions = new HashSet<float>();

            for (int i = 0; i < actionsProp.arraySize; i++)
            {
                if (goToTargets.ContainsKey(i))
                {
                    var pos = goToTargets[i];
                    _nodePositions[i] = new Vector2(pos.x, pos.y);
                    usedYPositions.Add(pos.y);
                }
                else
                {
                    // Skip Y positions used by GoTo targets
                    while (usedYPositions.Contains(currentY))
                        currentY += NodeSpacingY;

                    _nodePositions[i] = new Vector2(0f, currentY);
                    currentY += NodeSpacingY;
                }
            }
        }

        private void TraverseForLayout(SerializedProperty actionsProp, int index, int depth, float branchX,
            HashSet<int> visited, Dictionary<int, int> nodeDepth, Dictionary<int, float> nodeBranch, HashSet<int> currentPath)
        {
            if (index < 0 || index >= actionsProp.arraySize) return;
            if (visited.Contains(index)) return;
            if (currentPath.Contains(index)) return; // Prevent infinite loops

            visited.Add(index);
            currentPath.Add(index);
            nodeDepth[index] = depth;
            nodeBranch[index] = branchX;

            var actionProp = actionsProp.GetArrayElementAtIndex(index);
            var actionItemProp = actionProp.FindPropertyRelative("_actionItem");
            var runTypeProp = actionProp.FindPropertyRelative("RunType");
            var advancedRunTypeProp = actionProp.FindPropertyRelative("AdvancedRunType");
            var finishTypeProp = actionProp.FindPropertyRelative("FinishType");
            var goToProp = actionProp.FindPropertyRelative("GoTo");

            bool isSkip = runTypeProp != null && runTypeProp.enumValueIndex == 2;
            bool isAdvanced = advancedRunTypeProp != null && advancedRunTypeProp.boolValue;
            bool isWait = runTypeProp != null && runTypeProp.enumValueIndex == 0;
            bool isStop = isAdvanced && isWait && finishTypeProp != null && finishTypeProp.enumValueIndex == 1;
            bool isGoTo = isAdvanced && isWait && finishTypeProp != null && finishTypeProp.enumValueIndex == 3;

            // Check for If/Else
            if (actionItemProp != null && actionItemProp.managedReferenceValue is ActionIfElse ifElse)
            {
                var type = typeof(ActionIfElse);
                var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

                var trueBranch = (BranchType)type.GetField("_trueBranch", bindingFlags).GetValue(ifElse);
                var trueGoTo = (int)type.GetField("_trueGoTo", bindingFlags).GetValue(ifElse);
                var falseBranch = (BranchType)type.GetField("_falseBranch", bindingFlags).GetValue(ifElse);
                var falseGoTo = (int)type.GetField("_falseGoTo", bindingFlags).GetValue(ifElse);

                // Calculate branch positions (only GoTo branches out, Continue stays centered)
                float trueBranchX = trueBranch == BranchType.GoTo ? -NodeSpacingX * 0.6f : 0f;
                float falseBranchX = falseBranch == BranchType.GoTo ? NodeSpacingX * 0.6f : 0f;

                // True branch
                int trueTarget = trueBranch == BranchType.GoTo ? trueGoTo :
                                 trueBranch == BranchType.Continue && index + 1 < actionsProp.arraySize ? index + 1 : -1;
                if (trueTarget >= 0)
                    TraverseForLayout(actionsProp, trueTarget, depth + 1, trueBranchX, visited, nodeDepth, nodeBranch, new HashSet<int>(currentPath));

                // False branch
                int falseTarget = falseBranch == BranchType.GoTo ? falseGoTo :
                                  falseBranch == BranchType.Continue && index + 1 < actionsProp.arraySize ? index + 1 : -1;
                if (falseTarget >= 0 && falseTarget != trueTarget)
                    TraverseForLayout(actionsProp, falseTarget, depth + 1, falseBranchX, visited, nodeDepth, nodeBranch, new HashSet<int>(currentPath));

                currentPath.Remove(index);
                return;
            }

            // Skip nodes don't continue flow
            if (isSkip)
            {
                currentPath.Remove(index);
                return;
            }

            // Stop nodes end flow
            if (isStop)
            {
                currentPath.Remove(index);
                return;
            }

            // GoTo jumps to target
            if (isGoTo && goToProp != null)
            {
                TraverseForLayout(actionsProp, goToProp.intValue, depth + 1, branchX, visited, nodeDepth, nodeBranch, new HashSet<int>(currentPath));
                currentPath.Remove(index);
                return;
            }

            // Normal flow to next node - stay in column (reset to center)
            if (index + 1 < actionsProp.arraySize)
            {
                TraverseForLayout(actionsProp, index + 1, depth + 1, 0f, visited, nodeDepth, nodeBranch, new HashSet<int>(currentPath));
            }

            currentPath.Remove(index);
        }

        private void DrawNode(Rect rect, int index, string name, bool isSkip, bool isGoTo, bool isStop, bool isInProgress, Color? customColor)
        {
            // Node background
            Color bgColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            if (isInProgress)
                bgColor = new Color(0.15f, 0.4f, 0.15f, 1f);
            else if (isSkip)
                bgColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            else if (customColor.HasValue)
                bgColor = new Color(customColor.Value.r * 0.5f, customColor.Value.g * 0.5f, customColor.Value.b * 0.5f, 1f);
            else if (isGoTo)
                bgColor = new Color(0.2f, 0.3f, 0.4f, 1f);

            EditorGUI.DrawRect(rect, bgColor);

            // Node border
            Color borderColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            if (isInProgress)
                borderColor = new Color(0.2f, 0.9f, 0.2f, 1f);
            else if (customColor.HasValue)
                borderColor = customColor.Value;
            else if (isGoTo)
                borderColor = new Color(0.4f, 0.7f, 1f, 1f);

            DrawRectBorder(rect, borderColor, 2f);

            // Draw red line at bottom for Stop state
            if (isStop)
            {
                Rect stopLineRect = new Rect(rect.x, rect.yMax - 3f, rect.width, 2f);
                EditorGUI.DrawRect(stopLineRect, new Color(1f, 0.3f, 0.3f, 1f));
            }

            // Index circle
            float circleSize = 20f * _zoom;
            Rect circleRect = new Rect(rect.x - circleSize / 2, rect.y + (rect.height - circleSize) / 2, circleSize, circleSize);
            EditorGUI.DrawRect(circleRect, borderColor);

            // Index text
            GUIStyle indexStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(10 * _zoom),
                normal = { textColor = Color.white }
            };
            GUI.Label(circleRect, index.ToString(), indexStyle);

            // Node title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(12 * _zoom),
                fontStyle = FontStyle.Bold,
                normal = { textColor = isSkip ? Color.gray : Color.white }
            };

            string displayName = isSkip ? $"{name} [SKIP]" : name;
            GUI.Label(rect, displayName, titleStyle);

            // Double-click to select in inspector (single click is for dragging)
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2 && rect.Contains(Event.current.mousePosition))
            {
                Selection.activeGameObject = _target.gameObject;
                EditorGUIUtility.PingObject(_target);
                Event.current.Use();
            }
        }

        private void DrawConnections()
        {
            var serializedObject = new SerializedObject(_target);
            var actionsProp = serializedObject.FindProperty("Actions");

            if (actionsProp == null) return;

            Handles.BeginGUI();

            for (int i = 0; i < actionsProp.arraySize; i++)
            {
                var actionProp = actionsProp.GetArrayElementAtIndex(i);
                var actionItemProp = actionProp.FindPropertyRelative("_actionItem");
                var advancedRunTypeProp = actionProp.FindPropertyRelative("AdvancedRunType");
                var runTypeProp = actionProp.FindPropertyRelative("RunType");
                var finishTypeProp = actionProp.FindPropertyRelative("FinishType");
                var goToProp = actionProp.FindPropertyRelative("GoTo");

                bool isAdvanced = advancedRunTypeProp != null && advancedRunTypeProp.boolValue;
                bool isWait = runTypeProp != null && runTypeProp.enumValueIndex == 0;
                bool isGoTo = isAdvanced && isWait && finishTypeProp != null && finishTypeProp.enumValueIndex == 3;
                bool isSkip = runTypeProp != null && runTypeProp.enumValueIndex == 2;
                bool isStop = isAdvanced && isWait && finishTypeProp != null && finishTypeProp.enumValueIndex == 1;

                // Check if this is an IFlowControlAction (like If/Else)
                bool isFlowControl = false;
                if (actionItemProp != null && actionItemProp.managedReferenceValue is IFlowControlAction)
                {
                    isFlowControl = true;
                    var ifElse = actionItemProp.managedReferenceValue as ActionIfElse;
                    if (ifElse != null)
                    {
                        // Use reflection to access private fields
                        var type = typeof(ActionIfElse);
                        var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

                        var trueBranchField = type.GetField("_trueBranch", bindingFlags);
                        var trueGoToField = type.GetField("_trueGoTo", bindingFlags);
                        var falseBranchField = type.GetField("_falseBranch", bindingFlags);
                        var falseGoToField = type.GetField("_falseGoTo", bindingFlags);

                        var trueBranch = (BranchType)trueBranchField.GetValue(ifElse);
                        var trueGoTo = (int)trueGoToField.GetValue(ifElse);
                        var falseBranch = (BranchType)falseBranchField.GetValue(ifElse);
                        var falseGoTo = (int)falseGoToField.GetValue(ifElse);

                        // Draw true branch
                        if (trueBranch == BranchType.GoTo)
                        {
                            if (trueGoTo >= 0 && trueGoTo < _nodeRects.Count)
                                DrawBranchConnection(i, trueGoTo, new Color(0.3f, 0.8f, 0.3f, 1f), "T", 20f);
                        }
                        else if (trueBranch == BranchType.Continue)
                        {
                            if (i < _nodeRects.Count - 1)
                                DrawBranchConnection(i, i + 1, new Color(0.6f, 0.6f, 0.6f, 0.8f), "T", 20f);
                        }
                        // Stop = no connection

                        // Draw false branch
                        if (falseBranch == BranchType.GoTo)
                        {
                            if (falseGoTo >= 0 && falseGoTo < _nodeRects.Count)
                                DrawBranchConnection(i, falseGoTo, new Color(1f, 0.2f, 0.2f, 1f), "F", 40f);
                        }
                        else if (falseBranch == BranchType.Continue)
                        {
                            if (i < _nodeRects.Count - 1)
                                DrawBranchConnection(i, i + 1, new Color(0.6f, 0.6f, 0.6f, 0.8f), "F", 40f);
                        }
                        // Stop = no connection
                    }
                }

                if (isGoTo && goToProp != null)
                {
                    int targetIndex = goToProp.intValue;
                    if (targetIndex >= 0 && targetIndex < _nodeRects.Count && i < _nodeRects.Count)
                    {
                        DrawConnection(i, targetIndex);
                    }
                }

                // Draw flow connection to next node (unless Stop, Skip, GoTo, or FlowControl)
                if (i < actionsProp.arraySize - 1 && i < _nodeRects.Count - 1)
                {
                    if (!isSkip && !isStop && !isGoTo && !isFlowControl)
                    {
                        DrawFlowConnection(i, i + 1, false);
                    }
                }
            }

            Handles.EndGUI();
            serializedObject.Dispose();
        }

        private void DrawConnection(int fromIndex, int toIndex)
        {
            if (fromIndex >= _nodeRects.Count || toIndex >= _nodeRects.Count) return;

            Rect fromRect = _nodeRects[fromIndex];
            Rect toRect = _nodeRects[toIndex];

            // Draw from right side to right side (like inspector arrows)
            Vector2 start = new Vector2(fromRect.xMax, fromRect.center.y);
            Vector2 end = new Vector2(toRect.xMax, toRect.center.y);

            // Curve goes out to the right
            float curveOffset = 50f * _zoom + (fromIndex % 3) * 15f * _zoom;
            Vector2 startTangent = start + Vector2.right * curveOffset;
            Vector2 endTangent = end + Vector2.right * curveOffset;

            Color goToColor = new Color(0.4f, 0.7f, 1f, 1f);
            Handles.DrawBezier(start, end, startTangent, endTangent, goToColor, null, 3f);

            // Arrow head pointing left (into the target node)
            Vector2 dir = Vector2.left;
            DrawArrow(end, dir, goToColor);
        }

        private void DrawBranchConnection(int fromIndex, int toIndex, Color color, string label, float offsetX)
        {
            if (fromIndex >= _nodeRects.Count || toIndex >= _nodeRects.Count) return;

            Rect fromRect = _nodeRects[fromIndex];
            Rect toRect = _nodeRects[toIndex];

            // Vertical flow: top to bottom
            Vector2 start = new Vector2(fromRect.center.x, fromRect.yMax);
            Vector2 end = new Vector2(toRect.center.x, toRect.y);

            float tangentOffset = Mathf.Max(30f * _zoom, Mathf.Abs(end.y - start.y) * 0.4f);
            Vector2 startTangent = start + Vector2.up * tangentOffset;
            Vector2 endTangent = end + Vector2.down * tangentOffset;

            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, 2f);

            // Draw label near start (T on left, F on right)
            Vector2 labelPos = start + new Vector2((label == "T" ? -15f : 15f) * _zoom, 10f * _zoom);
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(10 * _zoom),
                normal = { textColor = color }
            };
            Rect labelRect = new Rect(labelPos.x - 10, labelPos.y - 10, 20, 20);
            GUI.Label(labelRect, label, labelStyle);

            // Arrow head pointing down
            Vector2 dir = (end - endTangent).normalized;
            DrawArrow(end, dir, color);
        }

        private void DrawFlowConnection(int fromIndex, int toIndex, bool hasGoTo)
        {
            if (fromIndex >= _nodeRects.Count || toIndex >= _nodeRects.Count) return;

            Rect fromRect = _nodeRects[fromIndex];
            Rect toRect = _nodeRects[toIndex];

            // Vertical flow: top to bottom
            Vector2 start = new Vector2(fromRect.center.x, fromRect.yMax);
            Vector2 end = new Vector2(toRect.center.x, toRect.y);

            float tangentOffset = Mathf.Max(20f * _zoom, Mathf.Abs(end.y - start.y) * 0.3f);
            Vector2 startTangent = start + Vector2.up * tangentOffset;
            Vector2 endTangent = end + Vector2.down * tangentOffset;

            Color flowColor = hasGoTo ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : new Color(0.6f, 0.6f, 0.6f, 0.8f);
            Handles.DrawBezier(start, end, startTangent, endTangent, flowColor, null, 2f);

            // Arrow head
            Vector2 dir = (end - endTangent).normalized;
            DrawArrow(end, dir, flowColor);
        }

        private void DrawArrow(Vector2 pos, Vector2 direction, Color color)
        {
            float arrowSize = 10f * _zoom;
            Vector2 right = new Vector2(-direction.y, direction.x);

            // Base of arrow at pos, tip extends in direction
            Vector2 p1 = pos + direction * arrowSize; // tip
            Vector2 p2 = pos + right * arrowSize * 0.5f; // base corner
            Vector2 p3 = pos - right * arrowSize * 0.5f; // base corner

            Color prevColor = Handles.color;
            Handles.color = color;
            Handles.DrawAAConvexPolygon(p1, p2, p3);
            Handles.color = prevColor;
        }

        private void DrawRectBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
