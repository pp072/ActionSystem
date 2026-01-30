using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using ActionSystem.Editor;


namespace ActionSystem
{
    [InitializeOnLoad]
    internal static class HierarchyIcons
    {
        private const int MAX_SELECTION_UPDATE_COUNT = 3;

        private static Dictionary<Type, GUIContent> _typeIcons = new()
        {
            { typeof(ActionList), EditorGUIUtility.IconContent("AnimatorStateTransition Icon") },
        };

        private static GUIContent _registryIcon = EditorGUIUtility.IconContent("d_Linked");
        private static GUIContent _registryWarningIcon = EditorGUIUtility.IconContent("console.warnicon.sml");

        private static Dictionary<int, List<GUIContent>> _labeledObjects = new();
        private static HashSet<int> _unlabeledObjects = new();
        private static GameObject[] _previousSelection = null;

        static HierarchyIcons()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;

            ObjectFactory.componentWasAdded += c => UpdateObject(c.gameObject.GetInstanceID());
            Selection.selectionChanged += OnSelectionChanged;
        }

        public static void RepaintHierarchyWindow()
        {
            _labeledObjects.Clear();
            _unlabeledObjects.Clear();
            EditorApplication.RepaintHierarchyWindow();
        }

        private static void OnHierarchyGUI(int id, Rect rect)
        {
            if (_unlabeledObjects.Contains(id)) return;

            if (ShouldDrawObject(id, out var icons))
            {
                var offset = 40;
                foreach (var icon in icons)
                {
                    rect.xMin = rect.xMax - offset;
                    GUI.Label(rect, icon);
                    offset += 20;
                }
            }
        }

        private static bool ShouldDrawObject(int id, out List<GUIContent> icon)
        {
            if (_labeledObjects.TryGetValue(id, out icon)) return true;
            return SortObject(id, out icon);
        }

        private static bool SortObject(int id, out List<GUIContent> icons)
        {
            var go = EditorUtility.InstanceIDToObject(id) as GameObject;
            icons = new List<GUIContent>();
            if (go == null) return false;

            foreach (var (type, typeIcon) in _typeIcons)
            {
                if (go.GetComponent(type))
                {
                    typeIcon.tooltip = type.Name;
                    icons.Add(typeIcon);
                }
            }

            // Handle RegisterToRegistry with duplicate detection
            var registry = go.GetComponent<RegisterToRegistry>();
            if (registry != null)
            {
                bool hasDuplicate = RegisterToRegistryEditor.HasDuplicateKey(registry.Key);
                var icon = hasDuplicate ? _registryWarningIcon : _registryIcon;
                icon.tooltip = hasDuplicate
                    ? $"RegisterToRegistry: \"{registry.Key}\" (DUPLICATE)"
                    : $"RegisterToRegistry: \"{registry.Key}\"";
                icons.Add(icon);
            }

            var hasIcons = icons.Count > 0;
            if (hasIcons)
            {
                _labeledObjects.Add(id, icons);
            }
            else
            {
                _unlabeledObjects.Add(id);
            }

            return hasIcons;
        }

        private static void UpdateObject(int id)
        {
            _unlabeledObjects.Remove(id);
            _labeledObjects.Remove(id);
            SortObject(id, out _);
        }


        private static void OnSelectionChanged()
        {
            TryUpdateObjects(_previousSelection);
            TryUpdateObjects(_previousSelection = Selection.gameObjects);
        }

        private static void TryUpdateObjects(GameObject[] objects)
        {
            if (objects != null && objects.Length > 0 && objects.Length <= MAX_SELECTION_UPDATE_COUNT)
                foreach (var go in objects)
                    UpdateObject(go.GetInstanceID());
        }
    }
}