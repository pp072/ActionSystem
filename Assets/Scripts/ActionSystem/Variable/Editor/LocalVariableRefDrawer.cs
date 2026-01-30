using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ActionSystem.Editor
{
    [CustomPropertyDrawer(typeof(LocalVariableRef), true)]
    public class LocalVariableRefDrawer : PropertyDrawer
    {
        private struct VariableOption
        {
            public string Name;
            public int Index;
            public bool IsGlobal;
            public string TypeName;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var useLocalVarProp = property.FindPropertyRelative("_useLocalVariable");
            var variableIndexProp = property.FindPropertyRelative("_variableIndex");
            var variableNameProp = property.FindPropertyRelative("_variableName");
            var isGlobalVarProp = property.FindPropertyRelative("_isGlobalVariable");

            // Check if this is a simple LocalVariableRef (no _useLocalVariable) or typed ref
            bool hasUseLocalVariable = useLocalVarProp != null;
            bool useLocalVariable = hasUseLocalVariable ? useLocalVarProp.boolValue : true;

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float toggleWidth = 18f;
            float modeLabelWidth = 14f;
            float labelWidth = EditorGUIUtility.labelWidth;

            // Draw label on the left
            Rect labelRect = new Rect(position.x, position.y, labelWidth, lineHeight);
            EditorGUI.LabelField(labelRect, label);

            // Calculate field area (after label)
            float fieldX = position.x + labelWidth + 2f;
            float fieldWidth = position.width - labelWidth - 2f;

            // Reserve space for toggle at the end
            if (hasUseLocalVariable)
            {
                fieldWidth -= toggleWidth + modeLabelWidth + 6f;
            }

            Rect fieldRect = new Rect(fieldX, position.y, fieldWidth, lineHeight);

            if (useLocalVariable)
            {
                // Get all variables (local + global) from ActionList
                var actionList = FindActionList(property);
                var variables = GetAllFilteredVariables(actionList, fieldInfo.FieldType);

                if (variables.Count == 0)
                {
                    var typeName = GetExpectedTypeName(fieldInfo.FieldType);
                    EditorGUI.HelpBox(fieldRect, $"No {typeName} variables", MessageType.Info);
                }
                else
                {
                    // Build dropdown options
                    var options = new List<string> { "(None)" };
                    options.AddRange(variables.ConvertAll(v =>
                        v.IsGlobal
                            ? $"[G:{v.Index}] {v.Name} ({v.TypeName})"
                            : $"[L:{v.Index}] {v.Name} ({v.TypeName})"));

                    // Find current selection
                    int currentSelection = 0;
                    bool currentIsGlobal = isGlobalVarProp?.boolValue ?? false;
                    for (int i = 0; i < variables.Count; i++)
                    {
                        if (variables[i].Index == variableIndexProp.intValue &&
                            variables[i].IsGlobal == currentIsGlobal)
                        {
                            currentSelection = i + 1; // +1 for "(None)"
                            break;
                        }
                    }

                    // Draw dropdown
                    int newSelection = EditorGUI.Popup(fieldRect, currentSelection, options.ToArray());

                    if (newSelection != currentSelection)
                    {
                        if (newSelection == 0)
                        {
                            variableIndexProp.intValue = -1;
                            variableNameProp.stringValue = "";
                            if (isGlobalVarProp != null) isGlobalVarProp.boolValue = false;
                        }
                        else
                        {
                            var selected = variables[newSelection - 1];
                            variableIndexProp.intValue = selected.Index;
                            variableNameProp.stringValue = selected.Name;
                            if (isGlobalVarProp != null) isGlobalVarProp.boolValue = selected.IsGlobal;
                        }
                    }
                }
            }
            else
            {
                // Draw direct reference field
                var directRefProp = property.FindPropertyRelative("_directReference");
                if (directRefProp == null)
                    directRefProp = property.FindPropertyRelative("_directValue");

                if (directRefProp != null)
                {
                    EditorGUI.PropertyField(fieldRect, directRefProp, GUIContent.none);
                }
            }

            // Draw toggle and label at the end
            if (hasUseLocalVariable)
            {
                float toggleX = fieldRect.xMax - 20f;

                // Draw mode label (V = Value, R = Reference)
                Rect modeLabelRect = new Rect(toggleX + 10f, position.y, toggleWidth + 10, lineHeight);
                EditorGUI.LabelField(modeLabelRect, useLocalVariable ? "R" : "V");

                // Draw toggle
                Rect toggleRect = new Rect(toggleX + 20f, position.y, toggleWidth + 10, lineHeight);
                useLocalVarProp.boolValue = EditorGUI.Toggle(toggleRect, useLocalVarProp.boolValue);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private ActionList FindActionList(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;

            if (targetObject is ActionList actionList)
                return actionList;

            if (targetObject is MonoBehaviour mb)
                return mb.GetComponent<ActionList>();

            return null;
        }

        private HashSet<LocalVariableType> GetCompatibleTypes(Type fieldType)
        {
            var types = new HashSet<LocalVariableType>();

            if (fieldType == typeof(FloatRef))
            {
                types.Add(LocalVariableType.Float);
                types.Add(LocalVariableType.Integer);
            }
            else if (fieldType == typeof(IntRef))
            {
                types.Add(LocalVariableType.Integer);
                types.Add(LocalVariableType.Float);
            }
            else if (fieldType == typeof(BoolRef))
            {
                types.Add(LocalVariableType.Boolean);
            }
            else if (fieldType == typeof(StringRef))
            {
                types.Add(LocalVariableType.String);
            }
            else if (fieldType == typeof(GameObjectRef))
            {
                types.Add(LocalVariableType.GameObject);
            }
            else if (fieldType == typeof(TransformRef))
            {
                types.Add(LocalVariableType.GameObject);
                types.Add(LocalVariableType.Component);
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(ComponentRef<>))
            {
                types.Add(LocalVariableType.Component);
                types.Add(LocalVariableType.GameObject);
            }
            else
            {
                // Base LocalVariableRef - show all
                foreach (LocalVariableType t in Enum.GetValues(typeof(LocalVariableType)))
                    types.Add(t);
            }

            return types;
        }

        private string GetExpectedTypeName(Type fieldType)
        {
            if (fieldType == typeof(FloatRef)) return "Float/Integer";
            if (fieldType == typeof(IntRef)) return "Integer/Float";
            if (fieldType == typeof(BoolRef)) return "Boolean";
            if (fieldType == typeof(StringRef)) return "String";
            if (fieldType == typeof(GameObjectRef)) return "GameObject";
            if (fieldType == typeof(TransformRef)) return "GameObject/Component";
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(ComponentRef<>))
                return "Component/GameObject";
            return "compatible";
        }

        private List<VariableOption> GetAllFilteredVariables(ActionList actionList, Type fieldType)
        {
            var result = new List<VariableOption>();

            if (actionList == null)
                return result;

            var compatibleTypes = GetCompatibleTypes(fieldType);

            // Add local variables
            var localVars = actionList.LocalVariables;
            for (int i = 0; i < localVars.Count; i++)
            {
                var v = localVars[i];
                if (compatibleTypes.Contains(v.Type))
                {
                    result.Add(new VariableOption
                    {
                        Name = v.Name,
                        Index = i,
                        IsGlobal = false,
                        TypeName = v.Type.ToString()
                    });
                }
            }

            // Add global variables (object types use registry key for runtime resolution)
            if (actionList.GlobalVariables != null)
            {
                var globalVars = actionList.GlobalVariables.Variables;
                for (int i = 0; i < globalVars.Count; i++)
                {
                    var v = globalVars[i];
                    if (compatibleTypes.Contains(v.Type))
                    {
                        result.Add(new VariableOption
                        {
                            Name = v.Name,
                            Index = i,
                            IsGlobal = true,
                            TypeName = v.Type.ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}
