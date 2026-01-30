using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Variable;

namespace ActionSystem.Editor
{
    public class RegistryOverviewWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<RegistryEntry> _entries = new();
        private List<VariablesScriptableObject> _globalVariables = new();
        private bool _showOnlyIssues = false;

        private class RegistryEntry
        {
            public string Key;
            public List<RegisterToRegistry> SceneObjects = new();
            public List<GlobalVariableRef> GlobalVariables = new();
            public bool HasDuplicate => SceneObjects.Count > 1;
            public bool IsMissing => SceneObjects.Count == 0 && GlobalVariables.Count > 0;
            public bool IsUnused => SceneObjects.Count > 0 && GlobalVariables.Count == 0;
            public bool HasIssue => HasDuplicate || IsMissing;
        }

        private class GlobalVariableRef
        {
            public VariablesScriptableObject Asset;
            public int VariableIndex;
            public string VariableName;
        }

        [MenuItem("Window/Action System/Registry Overview")]
        public static void ShowWindow()
        {
            var window = GetWindow<RegistryOverviewWindow>();
            window.titleContent = new GUIContent("Registry Overview");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            Refresh();
            EditorApplication.hierarchyChanged += Refresh;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= Refresh;
        }

        private void OnFocus()
        {
            Refresh();
        }

        private void Refresh()
        {
            _entries.Clear();
            var keyMap = new Dictionary<string, RegistryEntry>();

            // Find all RegisterToRegistry in scene
            var registrations = Object.FindObjectsByType<RegisterToRegistry>(FindObjectsSortMode.None);
            foreach (var reg in registrations)
            {
                if (string.IsNullOrEmpty(reg.Key)) continue;

                if (!keyMap.TryGetValue(reg.Key, out var entry))
                {
                    entry = new RegistryEntry { Key = reg.Key };
                    keyMap[reg.Key] = entry;
                }
                entry.SceneObjects.Add(reg);
            }

            // Find all global variable assets
            _globalVariables.Clear();
            var guids = AssetDatabase.FindAssets("t:VariablesScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<VariablesScriptableObject>(path);
                if (asset != null)
                {
                    _globalVariables.Add(asset);

                    for (int i = 0; i < asset.Variables.Count; i++)
                    {
                        var v = asset.Variables[i];
                        if ((v.Type == LocalVariableType.GameObject || v.Type == LocalVariableType.Component)
                            && !string.IsNullOrEmpty(v.RegistryKey))
                        {
                            if (!keyMap.TryGetValue(v.RegistryKey, out var entry))
                            {
                                entry = new RegistryEntry { Key = v.RegistryKey };
                                keyMap[v.RegistryKey] = entry;
                            }
                            entry.GlobalVariables.Add(new GlobalVariableRef
                            {
                                Asset = asset,
                                VariableIndex = i,
                                VariableName = v.Name
                            });
                        }
                    }
                }
            }

            _entries = keyMap.Values.OrderBy(e => e.Key).ToList();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawEntries();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Refresh();
            }

            GUILayout.Space(10);

            _showOnlyIssues = GUILayout.Toggle(_showOnlyIssues, "Show Issues Only", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            // Summary
            int issues = _entries.Count(e => e.HasIssue);
            var style = issues > 0 ? EditorStyles.boldLabel : EditorStyles.label;
            GUILayout.Label($"Keys: {_entries.Count}  Issues: {issues}", style);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEntries()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No registry keys found.\n\nAdd RegisterToRegistry components to scene objects or create global variables with registry keys.", MessageType.Info);
            }

            foreach (var entry in _entries)
            {
                if (_showOnlyIssues && !entry.HasIssue) continue;

                DrawEntry(entry);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntry(RegistryEntry entry)
        {
            // Determine status color
            Color bgColor = GUI.backgroundColor;
            if (entry.HasDuplicate)
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            else if (entry.IsMissing)
                GUI.backgroundColor = new Color(1f, 0.85f, 0.7f);
            else if (entry.IsUnused)
                GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = bgColor;

            // Header
            EditorGUILayout.BeginHorizontal();

            // Status icon
            GUIContent statusIcon;
            if (entry.HasDuplicate)
                statusIcon = EditorGUIUtility.IconContent("console.warnicon.sml");
            else if (entry.IsMissing)
                statusIcon = EditorGUIUtility.IconContent("console.erroricon.sml");
            else if (entry.IsUnused)
                statusIcon = EditorGUIUtility.IconContent("console.infoicon.sml");
            else
                statusIcon = EditorGUIUtility.IconContent("d_Linked");

            GUILayout.Label(statusIcon, GUILayout.Width(20), GUILayout.Height(18));
            EditorGUILayout.LabelField(entry.Key, EditorStyles.boldLabel);

            // Status text
            if (entry.HasDuplicate)
                GUILayout.Label("DUPLICATE", EditorStyles.miniLabel);
            else if (entry.IsMissing)
                GUILayout.Label("MISSING", EditorStyles.miniLabel);
            else if (entry.IsUnused)
                GUILayout.Label("Unused", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;

            // Scene objects
            if (entry.SceneObjects.Count > 0)
            {
                EditorGUILayout.LabelField("Scene Objects:", EditorStyles.miniBoldLabel);
                foreach (var reg in entry.SceneObjects)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);

                    if (reg != null)
                    {
                        if (GUILayout.Button(reg.gameObject.name, EditorStyles.linkLabel))
                        {
                            Selection.activeGameObject = reg.gameObject;
                            EditorGUIUtility.PingObject(reg.gameObject);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("(destroyed)", EditorStyles.miniLabel);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Scene Objects:", EditorStyles.miniBoldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // Global variables
            if (entry.GlobalVariables.Count > 0)
            {
                EditorGUILayout.LabelField("Global Variables:", EditorStyles.miniBoldLabel);
                foreach (var gv in entry.GlobalVariables)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);

                    if (gv.Asset != null)
                    {
                        if (GUILayout.Button($"{gv.Asset.name} / {gv.VariableName}", EditorStyles.linkLabel))
                        {
                            Selection.activeObject = gv.Asset;
                            EditorGUIUtility.PingObject(gv.Asset);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Global Variables:", EditorStyles.miniBoldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }
}
