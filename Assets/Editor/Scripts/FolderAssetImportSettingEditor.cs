using UnityEditor;
using UnityEngine;

namespace FolderAssetImporter
{
    [CustomEditor(typeof(DefaultAsset))]
    internal class FolderAssetImportSettingEditor : Editor
    {
        private static GUIStyle _style;
        private SerializedObject _serializedObject;
        private int _index;

        private static GUIStyle Style
        {
            get
            {
                _style ??= new GUIStyle(GUI.skin.window)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft,
                    padding =
                    {
                        top = 24
                    },
                    margin =
                    {
                        top = 4,
                        bottom = 4,
                        left = 4,
                        right = 4,
                    }
                };

                return _style;
            }
        }
        
        private static FolderAssetImportSettings Settings => FolderAssetImportSettings.Instance;

        private void OnEnable()
        {
            _index = Settings.GetIndex(target);
            _serializedObject = new SerializedObject(Settings);
            EditorApplication.focusChanged += OnWindowFocusChanged;
        }

        private void OnDisable()
        {
            _index = -1;
            _serializedObject = null;
            EditorApplication.focusChanged -= OnWindowFocusChanged;
        }

        private void OnWindowFocusChanged(bool hasFocus)
        {
            if (hasFocus)
            {
                _index = Settings.GetIndex(target);
                EditorUtility.SetDirty(target);
            }
            else
            {
                _index = -1;
                Settings.Save();
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (_index < 0 || _serializedObject == null) return;
            _serializedObject.Update();

            var suffix = $"_settings.Array.data[{_index}]";
            GUI.enabled = true;
            GUILayout.BeginVertical("Asset Presetting", Style);
            var enableAssetPresetting = _serializedObject.FindProperty($"{suffix}._enableAssetPresetting");
            GUI.enabled = enableAssetPresetting.boolValue =
                EditorGUILayout.ToggleLeft("Enable", enableAssetPresetting.boolValue);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_serializedObject.FindProperty($"{suffix}._assetPresettingRules"),
                new GUIContent("Rules"));
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

#if ENABLE_ADDRESSABLES
            GUI.enabled = true;
            GUILayout.BeginVertical("Address Naming", Style);
            var enableAddressNaming = _serializedObject.FindProperty($"{suffix}._enableAddressNaming");
            GUI.enabled = enableAddressNaming.boolValue =
                EditorGUILayout.ToggleLeft("Enable", enableAddressNaming.boolValue);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_serializedObject.FindProperty($"{suffix}._addressNamingRules"),
                new GUIContent("Rules"));
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUI.enabled = enableAddressNaming.boolValue;
#endif

            _serializedObject.ApplyModifiedProperties();
            GUI.enabled = GUI.enabled || enableAssetPresetting.boolValue;
            if (GUILayout.Button("Dry Run"))
            {
                var assetPath = AssetDatabase.GetAssetPath(target);
                Settings.ReImport(assetPath, true);
            }

            if (GUILayout.Button("Reimport"))
            {
                var assetPath = AssetDatabase.GetAssetPath(target);
                Settings.ReImport(assetPath);
            }
        }

        [MenuItem("CONTEXT/DefaultAsset/Clear")]
        private static void Clear()
        {
            Undo.RecordObject(Settings, "Clear");
            Settings.Clear();
        }

        [MenuItem("CONTEXT/DefaultAsset/Copy")]
        private static void Copy()
        {
            Settings.Copy();
        }

        [MenuItem("CONTEXT/DefaultAsset/Paste")]
        private static void Paste()
        {
            Undo.RecordObject(Settings, "Paste");
            Settings.Paste();
        }

        [MenuItem("CONTEXT/DefaultAsset/Paste", true)]
        private static bool CanPaste()
        {
            return Settings.CanPaste();
        }
    }
}