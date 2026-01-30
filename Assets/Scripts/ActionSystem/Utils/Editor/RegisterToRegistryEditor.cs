using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ActionSystem.Editor
{
    [CustomEditor(typeof(RegisterToRegistry))]
    public class RegisterToRegistryEditor : UnityEditor.Editor
    {
        private static Dictionary<string, List<RegisterToRegistry>> _keyUsage = new();
        private static bool _cacheValid = false;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var reg = (RegisterToRegistry)target;
            var key = reg.Key;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Unique ID"))
            {
                Undo.RecordObject(reg, "Generate Unique Registry Key");
                var shortGuid = System.Guid.NewGuid().ToString("N").Substring(0, 8);
                var newKey = $"{reg.gameObject.name}_{shortGuid}";
                var keyProp = new SerializedObject(reg).FindProperty("_key");
                keyProp.stringValue = newKey;
                keyProp.serializedObject.ApplyModifiedProperties();
                InvalidateCache();
            }
            if (!string.IsNullOrEmpty(key) && GUILayout.Button("Copy Key", GUILayout.Width(70)))
            {
                EditorGUIUtility.systemCopyBuffer = key;
            }
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(key))
            {
                EditorGUILayout.HelpBox("Key is empty. This object won't be registered.", MessageType.Info);
                return;
            }

            // Check for duplicates
            var duplicates = GetDuplicates(key, reg);
            if (duplicates.Count > 0)
            {
                var names = new List<string>();
                foreach (var dup in duplicates)
                {
                    if (dup != null)
                        names.Add(dup.gameObject.name);
                }

                EditorGUILayout.HelpBox(
                    $"Duplicate key \"{key}\" also used by:\n{string.Join("\n", names)}",
                    MessageType.Warning);

                if (GUILayout.Button("Select Duplicates"))
                {
                    var objects = new List<GameObject> { reg.gameObject };
                    foreach (var dup in duplicates)
                    {
                        if (dup != null)
                            objects.Add(dup.gameObject);
                    }
                    Selection.objects = objects.ToArray();
                }
            }
        }

        private List<RegisterToRegistry> GetDuplicates(string key, RegisterToRegistry self)
        {
            RefreshCache();

            var duplicates = new List<RegisterToRegistry>();

            if (_keyUsage.TryGetValue(key, out var list))
            {
                foreach (var reg in list)
                {
                    if (reg != null && reg != self)
                        duplicates.Add(reg);
                }
            }

            return duplicates;
        }

        public static void InvalidateCache()
        {
            _cacheValid = false;
            HierarchyIcons.RepaintHierarchyWindow();
        }

        private static void RefreshCache()
        {
            if (_cacheValid) return;

            _keyUsage.Clear();

            var all = Object.FindObjectsByType<RegisterToRegistry>(FindObjectsSortMode.None);
            foreach (var reg in all)
            {
                if (string.IsNullOrEmpty(reg.Key)) continue;

                if (!_keyUsage.TryGetValue(reg.Key, out var list))
                {
                    list = new List<RegisterToRegistry>();
                    _keyUsage[reg.Key] = list;
                }
                list.Add(reg);
            }

            _cacheValid = true;
        }

        public static bool HasDuplicateKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            RefreshCache();

            return _keyUsage.TryGetValue(key, out var list) && list.Count > 1;
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.hierarchyChanged += InvalidateCache;
            Undo.postprocessModifications += OnPostprocessModifications;
        }

        private static UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            foreach (var mod in modifications)
            {
                if (mod.currentValue?.target is RegisterToRegistry)
                {
                    InvalidateCache();
                    break;
                }
            }
            return modifications;
        }
    }
}
