using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ActionSystem;
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
            string currentBox = null;
            while (!SerializedProperty.EqualContents(child, end))
            {
                if (!ShouldDrawField(child, property.managedReferenceValue))
                {
                    child.NextVisible(false);
                    continue;
                }
                string box = GetBoxGroup(child, property.managedReferenceValue);
                if (box != currentBox)
                {
                    currentBox = box;
                    if (box != null) height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }
                height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
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
            InspectorRepaintScheduler.Request();

            // ---- HEADER LINE (NO ARROW) ----
            var line = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(line, label);

            Rect dropdownRect = line;
            dropdownRect.x += EditorGUIUtility.labelWidth;
            dropdownRect.width -= EditorGUIUtility.labelWidth;

            var referenceType =
                ReflectionUtils.ExtractTypeFromString(property.managedReferenceFullTypename);
            
            var isHaveOtherReference = IsHaveSameOtherSerializeReference(property, out _);

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
            
            if (isHaveOtherReference)
            {
                // GUI.backgroundColor = Color.brown;
                // if (GUI.Button(GetFixCrossReferencesRect(dropdownRect), "Fix"))
                {
                    FixCrossReference(property);
                }
            }

            // ---- CHILDREN (normal arrows preserved) ----
            EditorGUI.indentLevel++;

            var child = property.Copy();
            var end = child.GetEndProperty();

            line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            string currentBox = null;
            child.NextVisible(true);
            while (!SerializedProperty.EqualContents(child, end))
            {
                if (!ShouldDrawField(child, property.managedReferenceValue))
                {
                    child.NextVisible(false);
                    continue;
                }

                // BoxGroup label when group changes
                string newBox = GetBoxGroup(child, property.managedReferenceValue);
                if (newBox != currentBox)
                {
                    currentBox = newBox;
                    if (newBox != null)
                    {
                        line.height = EditorGUIUtility.singleLineHeight;
                        EditorGUI.LabelField(line, newBox, EditorStyles.boldLabel);
                        line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }

                var h = EditorGUI.GetPropertyHeight(child, true);
                line.height = h;

                if (!TryDrawDropdown(ref line, child, property.managedReferenceValue))
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
                var attr = type.GetCustomAttribute<ActionMenuPathAttribute>();
                return attr.MenuPath;
            }

            return String.Empty;
        }

        public static string GetActionName(Type type)
        {
            if (type == null)
            {
                return String.Empty;
            }

            var typesWithMenuPath = TypeCache.GetTypesWithAttribute(typeof(ActionMenuPathAttribute));
            if (typesWithMenuPath.Contains(type))
            {
                var attr = type.GetCustomAttribute<ActionMenuPathAttribute>();
                return attr.Name;
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

        // ---- Conditional drawing helpers ----

        private static bool ShouldDrawField(SerializedProperty child, object managedRef)
        {
            if (managedRef == null) return true;
            var fi = managedRef.GetType().GetField(child.name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi == null) return true;
            var showIf = fi.GetCustomAttribute<ShowIfAttribute>();
            if (showIf != null) return EvalCondition(showIf.Condition, managedRef);
            var hideIf = fi.GetCustomAttribute<HideIfAttribute>();
            if (hideIf != null) return !EvalCondition(hideIf.Condition, managedRef);
            return true;
        }

        private static bool EvalCondition(string name, object target)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var type = target.GetType();
            return (bool)(type.GetProperty(name, flags)?.GetValue(target)
                ?? type.GetField(name, flags)?.GetValue(target)
                ?? type.GetMethod(name, flags)?.Invoke(target, null)
                ?? (object)true);
        }

        private static string GetBoxGroup(SerializedProperty child, object managedRef)
        {
            if (managedRef == null) return null;
            var fi = managedRef.GetType().GetField(child.name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return fi?.GetCustomAttribute<BoxGroupAttribute>()?.Name;
        }

        private static bool TryDrawDropdown(ref Rect line, SerializedProperty child, object managedRef)
        {
            if (managedRef == null) return false;
            var fi = managedRef.GetType().GetField(child.name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var dropdownAttr = fi?.GetCustomAttribute<DropdownAttribute>();
            if (dropdownAttr == null) return false;

            var method = managedRef.GetType().GetMethod(dropdownAttr.Provider, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method == null) return false;

            var items = method.Invoke(managedRef, null) as IReadOnlyList<ActionInfo>;
            if (items == null || items.Count == 0) return false;

            var labels = items.Select(x => x.name).ToArray();
            var values = items.Select(x => x.index).ToArray();
            int currentVal = child.intValue;
            int currentIdx = Array.IndexOf(values, currentVal);
            if (currentIdx < 0) currentIdx = 0;

            float h = EditorGUIUtility.singleLineHeight;
            line.height = h;
            float lw = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(new Rect(line.x, line.y, lw, h), ObjectNames.NicifyVariableName(child.name));
            int newIdx = EditorGUI.Popup(new Rect(line.x + lw, line.y, line.width - lw, h), currentIdx, labels);
            if (newIdx != currentIdx)
                child.intValue = values[newIdx];

            return true;
        }
    }
}