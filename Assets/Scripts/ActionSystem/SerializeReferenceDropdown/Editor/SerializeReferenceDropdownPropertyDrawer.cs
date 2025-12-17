using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SerializeReferenceDropdown.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceDropdown.Editor
{
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public class SerializeReferenceDropdownPropertyDrawer : PropertyDrawer
    {
        private const string NullName = "null";
        private List<Type> assignableTypes;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            var child = property.Copy();
            var end = child.GetEndProperty();

            child.NextVisible(true);
            while (!SerializedProperty.EqualContents(child, end))
            {
                height += EditorGUI.GetPropertyHeight(child, true)
                          + EditorGUIUtility.standardVerticalSpacing;
                child.NextVisible(false);
            }

            return height;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            GUI.backgroundColor = Color.aliceBlue;
            GUI.Box(rect, "");
            EditorGUI.BeginProperty(rect, label, property);

            var indent = EditorGUI.indentLevel --;
            EditorGUI.indentLevel = 0;

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                DrawIMGUITypeDropdown(rect, property, label);
            }
            else
            {
                //EditorGUI.PropertyField(rect, property, label, true);
            }

            EditorGUI.indentLevel = indent ++;
            EditorGUI.EndProperty();
        }
        
        private void DrawIMGUITypeDropdown(Rect rect, SerializedProperty property, GUIContent label)
        {
            assignableTypes ??= GetAssignableTypes(property);

            property.isExpanded = true; // keep children visible

            // ---- HEADER LINE (NO ARROW) ----
            var line = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(line, label);

            Rect dropdownRect = line;
            dropdownRect.x += EditorGUIUtility.labelWidth;
            dropdownRect.width -= EditorGUIUtility.labelWidth;

            var referenceType =
                ReflectionUtils.ExtractTypeFromString(property.managedReferenceFullTypename);

            if (EditorGUI.DropdownButton(
                    dropdownRect,
                    new GUIContent(GetActionName(referenceType)),
                    FocusType.Keyboard))
            {
                var dropdown = new SerializeReferenceDropdownAdvancedDropdown(
                    new AdvancedDropdownState(),
                    assignableTypes,
                    index => WriteNewInstanceByIndexType(index, property));

                dropdown.Show(dropdownRect);
            }

            // ---- CHILDREN (normal arrows preserved) ----
            EditorGUI.indentLevel++;

            var child = property.Copy();
            var end = child.GetEndProperty();

            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            child.NextVisible(true);
            while (!SerializedProperty.EqualContents(child, end))
            {
                var h = EditorGUI.GetPropertyHeight(child, true);
                line.height = h;

                EditorGUI.PropertyField(line, child, true);

                line.y += h + EditorGUIUtility.standardVerticalSpacing;
                child.NextVisible(false);
            }

            EditorGUI.indentLevel--;
        }
        
        private void FixCrossReference(SerializedProperty property)
        {
            var json = JsonUtility.ToJson(property.managedReferenceValue);
            WriteNewInstanceByType(property.managedReferenceValue.GetType(), property);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
            
            JsonUtility.FromJsonOverwrite(json, property.managedReferenceValue);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
            
            targetObjectAndSerializeReferencePaths.Clear();
           
        }

        public static string GetTypeName(Type type) => type == null ? NullName : ObjectNames.NicifyVariableName(type.Name);
        
        
        private static readonly Dictionary<Object, HashSet<string>> targetObjectAndSerializeReferencePaths =
            new Dictionary<Object, HashSet<string>>();
        
        private bool IsHaveSameOtherSerializeReference(SerializedProperty property, out bool isNewElement)
        {
            isNewElement = false;

            if (property.managedReferenceValue == null)
            {
                return false;
            }

            var target = property.serializedObject.targetObject;
            if (targetObjectAndSerializeReferencePaths.TryGetValue(target, out var serializeReferencePaths) == false)
            {
                isNewElement = true;
                serializeReferencePaths = new HashSet<string>();
                targetObjectAndSerializeReferencePaths.Add(target, serializeReferencePaths);
            }

            // Can't find this path in serialized object. Example - new element in array
            if (serializeReferencePaths.Contains(property.propertyPath) == false)
            {
                isNewElement = true;
                var paths = FindAllSerializeReferencePathsInTargetObject();
                serializeReferencePaths.Clear();
                foreach (var path in paths)
                {
                    serializeReferencePaths.Add(path);
                }
            }

            foreach (var referencePath in serializeReferencePaths)
            {
                if (property.propertyPath == referencePath)
                {
                    continue;
                }

                using var otherProperty = property.serializedObject.FindProperty(referencePath);
                if (otherProperty != null)
                {
                    if (otherProperty.managedReferenceId == property.managedReferenceId)
                    {
                        return true;
                    }
                }
            }

            return false;

            HashSet<string> FindAllSerializeReferencePathsInTargetObject()
            {
                var paths = new HashSet<string>();
                SOUtils.TraverseSO(property.serializedObject.targetObject, FillAllPaths);
                return paths;

                bool FillAllPaths(SerializedProperty serializeReferenceProperty)
                {
                    paths.Add(serializeReferenceProperty.propertyPath);
                    return false;
                }
            }
        }
        public static string GetTypeMenuPath(Type type)
        {
            if (type == null)
            {
                return String.Empty;
            }

            var typesWithMenuPath = TypeCache.GetTypesWithAttribute(typeof(ActionMenuPathAttribute));
            if (typesWithMenuPath.Contains(type))
            {
                var tooltipAttribute = type.GetCustomAttribute<ActionMenuPathAttribute>();
                return tooltipAttribute.tooltip;
            }

            return String.Empty;
        }
        
        public static string GetActionName(Type type)
        {
            if (type == null)
            {
                return String.Empty;
            }

            var typesWithTooltip = TypeCache.GetTypesWithAttribute(typeof(ActionNameAttribute));
            if (typesWithTooltip.Contains(type))
            {
                var tooltipAttribute = type.GetCustomAttribute<ActionNameAttribute>();
                return tooltipAttribute.tooltip;
            }

            return String.Empty;
        }

        private List<Type> GetAssignableTypes(SerializedProperty property)
        {
            var propertyType = ReflectionUtils.ExtractTypeFromString(property.managedReferenceFieldTypename);
            var allTypes = ReflectionUtils.GetAllTypesInCurrentDomain();
            var targetAssignableTypes = ReflectionUtils.GetFinalAssignableTypes(propertyType, allTypes,
                predicate: type => type.IsSubclassOf(typeof(UnityEngine.Object)) == false).ToList();
            targetAssignableTypes.Insert(0, null);
            return targetAssignableTypes;
        }

        private void WriteNewInstanceByType(Type type, SerializedProperty property)
        {
            //var newType = assignableTypes[typeIndex];
            var newObject = type != null ? Activator.CreateInstance(type) : null;
            if(newObject == null) return;
            ApplyValueToProperty(newObject, property);
        }
        private void WriteNewInstanceByIndexType(int typeIndex, SerializedProperty property)
        {
            var newType = assignableTypes[typeIndex];
            var newObject = newType != null ? Activator.CreateInstance(newType) : null;
            if(newObject == null) return;
            ApplyValueToProperty(newObject, property);
        }

        private void ApplyValueToProperty(object value, SerializedProperty property)
        {
            property.managedReferenceValue = value;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
            //property.isExpanded = true;
        }
    }
}