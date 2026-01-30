using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionSystem.Editor
{
    [CustomPropertyDrawer(typeof(LocalVariable))]
    public class LocalVariableDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var typeProp = property.FindPropertyRelative("_type");
            var componentProp = property.FindPropertyRelative("_componentValue");

            // Check if we're in a ScriptableObject (can't hold scene references)
            bool isInScriptableObject = property.serializedObject.targetObject is ScriptableObject;

            // Handle drag and drop for Component type (only if not in ScriptableObject)
            if (!isInScriptableObject && typeProp != null && componentProp != null &&
                typeProp.enumValueIndex == (int)LocalVariableType.Component)
            {
                HandleComponentDragAndDrop(position, componentProp);
            }

            if (isInScriptableObject)
            {
                // Custom drawing for ScriptableObject - filter type options
                DrawForScriptableObject(position, property, label, typeProp);
            }
            else
            {
                // Draw default property
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        private void DrawForScriptableObject(Rect position, SerializedProperty property, GUIContent label, SerializedProperty typeProp)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // Draw foldout
            Rect foldoutRect = new Rect(position.x, position.y, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float yOffset = lineHeight + spacing;

                // Draw Name
                var nameProp = property.FindPropertyRelative("_name");
                Rect nameRect = new Rect(position.x, position.y + yOffset, position.width, lineHeight);
                EditorGUI.PropertyField(nameRect, nameProp);
                yOffset += lineHeight + spacing;

                // Draw Type dropdown (all types)
                Rect typeRect = new Rect(position.x, position.y + yOffset, position.width, lineHeight);
                EditorGUI.PropertyField(typeRect, typeProp);
                yOffset += lineHeight + spacing;

                // Draw value field or registry key based on type
                var currentType = (LocalVariableType)typeProp.enumValueIndex;

                if (currentType == LocalVariableType.GameObject || currentType == LocalVariableType.Component)
                {
                    // For object types in ScriptableObject, show registry key dropdown
                    var registryKeyProp = property.FindPropertyRelative("_registryKey");
                    if (registryKeyProp != null)
                    {
                        Rect keyRect = new Rect(position.x, position.y + yOffset, position.width, lineHeight);
                        DrawRegistryKeyDropdown(keyRect, registryKeyProp, currentType);
                    }
                }
                else
                {
                    // For primitive types, show value field
                    SerializedProperty valueProp = GetValueProperty(property, currentType);
                    if (valueProp != null)
                    {
                        Rect valueRect = new Rect(position.x, position.y + yOffset, position.width, lineHeight);
                        EditorGUI.PropertyField(valueRect, valueProp);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        private SerializedProperty GetValueProperty(SerializedProperty property, LocalVariableType type)
        {
            return type switch
            {
                LocalVariableType.Integer => property.FindPropertyRelative("_intValue"),
                LocalVariableType.Float => property.FindPropertyRelative("_floatValue"),
                LocalVariableType.Boolean => property.FindPropertyRelative("_boolValue"),
                LocalVariableType.String => property.FindPropertyRelative("_stringValue"),
                _ => null
            };
        }

        private void DrawRegistryKeyDropdown(Rect rect, SerializedProperty registryKeyProp, LocalVariableType type)
        {
            var keys = GetRegisteredKeys(type);
            var currentKey = registryKeyProp.stringValue;

            // Build options list
            var options = new List<string> { "(None)", "(Custom...)" };
            options.AddRange(keys);

            // Find current selection
            int currentIndex = 0;
            if (!string.IsNullOrEmpty(currentKey))
            {
                int keyIndex = keys.IndexOf(currentKey);
                if (keyIndex >= 0)
                    currentIndex = keyIndex + 2; // +2 for "(None)" and "(Custom...)"
                else
                    currentIndex = 1; // Custom value
            }

            // Calculate rects for label, popup, and button
            float labelWidth = EditorGUIUtility.labelWidth;
            float buttonWidth = 50f;
            bool hasKey = !string.IsNullOrEmpty(currentKey);

            Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            float popupWidth = rect.width - labelWidth - 2 - buttonWidth - 2;
            Rect popupRect = new Rect(rect.x + labelWidth + 2, rect.y, popupWidth, rect.height);
            Rect selectRect = new Rect(popupRect.xMax + 2, rect.y, buttonWidth, rect.height);

            EditorGUI.LabelField(labelRect, "Registry Key");

            // If custom value, show text field with dropdown button
            if (currentIndex == 1 || (currentIndex == 0 && !string.IsNullOrEmpty(currentKey) && !keys.Contains(currentKey)))
            {
                float dropdownButtonWidth = 20f;
                Rect fieldRect = new Rect(popupRect.x, popupRect.y, popupRect.width - dropdownButtonWidth - 2, popupRect.height);
                Rect buttonRect = new Rect(fieldRect.xMax + 2, popupRect.y, dropdownButtonWidth, popupRect.height);

                registryKeyProp.stringValue = EditorGUI.TextField(fieldRect, currentKey);

                if (EditorGUI.DropdownButton(buttonRect, GUIContent.none, FocusType.Passive))
                {
                    ShowKeyMenu(registryKeyProp, keys);
                }
            }
            else
            {
                int newIndex = EditorGUI.Popup(popupRect, currentIndex, options.ToArray());

                if (newIndex != currentIndex)
                {
                    if (newIndex == 0)
                        registryKeyProp.stringValue = "";
                    else if (newIndex == 1)
                        registryKeyProp.stringValue = currentKey; // Keep current, switch to text field mode
                    else
                        registryKeyProp.stringValue = keys[newIndex - 2];
                }
            }

            // Draw Select or Paste button
            if (hasKey)
            {
                if (GUI.Button(selectRect, "Select"))
                {
                    SelectObjectByKey(currentKey);
                }
            }
            else
            {
                // Show Paste button when no key is set
                string clipboard = EditorGUIUtility.systemCopyBuffer;
                bool hasClipboard = !string.IsNullOrEmpty(clipboard);

                using (new EditorGUI.DisabledScope(!hasClipboard))
                {
                    if (GUI.Button(selectRect, "Paste"))
                    {
                        registryKeyProp.stringValue = clipboard;
                    }
                }
            }
        }

        private void SelectObjectByKey(string key)
        {
            var registrations = Object.FindObjectsByType<RegisterToRegistry>(FindObjectsSortMode.None);

            foreach (var reg in registrations)
            {
                if (reg.Key == key)
                {
                    Selection.activeGameObject = reg.gameObject;
                    EditorGUIUtility.PingObject(reg.gameObject);
                    return;
                }
            }

            Debug.LogWarning($"No object found with registry key: {key}");
        }

        private void ShowKeyMenu(SerializedProperty registryKeyProp, List<string> keys)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("(None)"), false, () =>
            {
                registryKeyProp.stringValue = "";
                registryKeyProp.serializedObject.ApplyModifiedProperties();
            });

            menu.AddSeparator("");

            foreach (var key in keys)
            {
                var k = key;
                menu.AddItem(new GUIContent(k), registryKeyProp.stringValue == k, () =>
                {
                    registryKeyProp.stringValue = k;
                    registryKeyProp.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        private List<string> GetRegisteredKeys(LocalVariableType type)
        {
            var keys = new HashSet<string>();

            // Find all RegisterToRegistry components in loaded scenes
            var registrations = Object.FindObjectsByType<RegisterToRegistry>(FindObjectsSortMode.None);

            foreach (var reg in registrations)
            {
                if (!string.IsNullOrEmpty(reg.Key))
                    keys.Add(reg.Key);
            }

            return keys.OrderBy(k => k).ToList();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool isInScriptableObject = property.serializedObject.targetObject is ScriptableObject;

            if (isInScriptableObject)
            {
                if (!property.isExpanded)
                    return EditorGUIUtility.singleLineHeight;

                // Foldout + Name + Type + Value = 4 lines
                return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4;
            }

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private void HandleComponentDragAndDrop(Rect position, SerializedProperty componentProp)
        {
            Event evt = Event.current;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (!position.Contains(evt.mousePosition))
                    return;

                var draggedObject = DragAndDrop.objectReferences.FirstOrDefault();
                if (draggedObject == null)
                    return;

                GameObject gameObject = null;

                if (draggedObject is GameObject go)
                {
                    gameObject = go;
                }
                else if (draggedObject is Component comp)
                {
                    // If a component is dragged directly, just accept it
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        componentProp.objectReferenceValue = comp;
                        componentProp.serializedObject.ApplyModifiedProperties();
                    }
                    else
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    }
                    evt.Use();
                    return;
                }

                if (gameObject != null)
                {
                    var components = gameObject.GetComponents<Component>()
                        .Where(c => c != null && !(c is Transform))
                        .ToArray();

                    if (components.Length > 0)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();

                            // Capture serialized object for menu callback
                            var serializedObject = componentProp.serializedObject;
                            var propertyPath = componentProp.propertyPath;

                            // Show component selection menu
                            ShowComponentMenu(components, serializedObject, propertyPath);
                        }
                        evt.Use();
                    }
                }
            }
        }

        private void ShowComponentMenu(Component[] components, SerializedObject serializedObject, string propertyPath)
        {
            var menu = new GenericMenu();

            foreach (var component in components)
            {
                var comp = component; // Capture for closure
                var typeName = comp.GetType().Name;
                menu.AddItem(new GUIContent(typeName), false, () =>
                {
                    // Re-find the property in case serialized object was modified
                    serializedObject.Update();
                    var prop = serializedObject.FindProperty(propertyPath);
                    if (prop != null)
                    {
                        prop.objectReferenceValue = comp;
                        serializedObject.ApplyModifiedProperties();
                    }
                });
            }

            menu.ShowAsContext();
        }
    }
}
