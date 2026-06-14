using UnityEditor;
using UnityEngine;

namespace FolderAssetImporter
{
    [CustomEditor(typeof(DefaultAsset))]
    internal class FolderAssetImportSettingEditor : Editor
    {
        private static SerializedObject _serializedObject;
        private static GUIStyle _style;

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

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(FolderAssetImportSettings.Instance);
            FolderAssetImportSettings.Instance.Select(target);
        }

        private void OnDisable()
        {
            FolderAssetImportSettings.Instance.Save();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            _serializedObject.Update();

            GUI.enabled = true;
            GUILayout.BeginVertical("Asset Presetting", Style);
            var enableAssetPresetting = _serializedObject.FindProperty("_selected._enableAssetPresetting");
            GUI.enabled = enableAssetPresetting.boolValue =
                EditorGUILayout.ToggleLeft("Enable", enableAssetPresetting.boolValue);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("_selected._assetPresettingRules"),
                new GUIContent("Rules"));
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

#if ENABLE_ADDRESSABLES
            GUI.enabled = true;
            GUILayout.BeginVertical("Address Naming", Style);
            var enableAddressNaming = _serializedObject.FindProperty("_selected._enableAddressNaming");
            GUI.enabled = enableAddressNaming.boolValue =
                EditorGUILayout.ToggleLeft("Enable", enableAddressNaming.boolValue);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("_selected._addressNamingRules"),
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
                FolderAssetImportSettings.Instance.ReImport(assetPath, true);
            }

            if (GUILayout.Button("Reimport"))
            {
                var assetPath = AssetDatabase.GetAssetPath(target);
                FolderAssetImportSettings.Instance.ReImport(assetPath);
            }
        }

        [MenuItem("CONTEXT/DefaultAsset/Clear")]
        private static void Clear()
        {
            FolderAssetImportSettings.Instance.Clear();
        }

        [MenuItem("CONTEXT/DefaultAsset/Copy")]
        private static void Copy()
        {
            FolderAssetImportSettings.Instance.Copy();
        }

        [MenuItem("CONTEXT/DefaultAsset/Paste")]
        private static void Paste()
        {
            FolderAssetImportSettings.Instance.Paste();
        }

        [MenuItem("CONTEXT/DefaultAsset/Paste", true)]
        private static bool CanPaste()
        {
            return FolderAssetImportSettings.Instance.CanPaste();
        }
    }
}