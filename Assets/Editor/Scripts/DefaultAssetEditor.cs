using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FolderAssetImporter
{
    [CustomEditor(typeof(DefaultAsset))]
    public class DefaultAssetEditor : Editor
    {
        private static FolderAssetImportSetting.Wrapper _wrapper;
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
            var assetPath = AssetDatabase.GetAssetPath(target);
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null) return;
            if (_wrapper == null)
            {
                _wrapper = CreateInstance<FolderAssetImportSetting.Wrapper>();
                _serializedObject = new SerializedObject(_wrapper);
            }

            _wrapper.Setup(importer);
        }

        private void OnDisable()
        {
            _wrapper.Save();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            _serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            GUI.enabled = true;
            GUILayout.BeginVertical("Asset Presetting", Style);
            var enableAssetPresetting = _serializedObject.FindProperty("_setting._enableAssetPresetting");
            GUI.enabled = enableAssetPresetting.boolValue =
                EditorGUILayout.ToggleLeft("Enable", enableAssetPresetting.boolValue);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("_setting._assetPresettingRules"),
                new GUIContent("Rules"));
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

#if ENABLE_ADDRESSABLES
            GUI.enabled = true;
            GUILayout.BeginVertical("Address Naming", Style);
            var enableAddressNaming = _serializedObject.FindProperty("_setting._enableAddressNaming");
            GUI.enabled = enableAddressNaming.boolValue =
                EditorGUILayout.ToggleLeft("Enable", enableAddressNaming.boolValue);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("_setting._addressNamingRules"),
                new GUIContent("Rules"));
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
            GUI.enabled = enableAddressNaming.boolValue;
#endif
            if (EditorGUI.EndChangeCheck())
            {
                _wrapper.SetChanged();
            }

            _serializedObject.ApplyModifiedProperties();
            GUI.enabled = GUI.enabled || enableAssetPresetting.boolValue;
            if (GUILayout.Button("Dry Run"))
            {
                _wrapper.ReImport(true);
            }

            if (GUILayout.Button("Reimport"))
            {
                _wrapper.ReImport();
            }
        }
        
        [MenuItem("CONTEXT/DefaultAsset/Clear")]
        private static void Clear()
        {
            _wrapper.Clear();
        }

        [MenuItem("CONTEXT/DefaultAsset/Copy")]
        private static void Copy()
        {
            _wrapper.Copy();
        }

        [MenuItem("CONTEXT/DefaultAsset/Paste")]
        private static void Paste()
        {
            _wrapper.Paste();
        }
        
        [MenuItem("CONTEXT/DefaultAsset/Paste", true)]
        private static bool CanPaste()
        {
            return _wrapper.CanPaste();
        }
    }
    
    
    public class GUIStyleChecker : EditorWindow
    {
        [MenuItem("Tools/GUIStyleChecker")]
        private static void ShowWindow()
        {
            var window = GetWindow<GUIStyleChecker>();
            window.titleContent = new GUIContent("GUIStyleChecker");
            window.Show();
        }
 
        private List<GUIStyle> _editorGUIStyles;
        private Vector2 _position;
 
        private void Init()
        {
            if (_editorGUIStyles != null)
                return;
        
            _editorGUIStyles = new List<GUIStyle>();
            var e = GUI.skin.GetEnumerator();
            while (e.MoveNext())
            {
                try
                {
                    _editorGUIStyles.Add(e.Current as GUIStyle);
                }
                catch
                {
                    // ignored
                }
            }
        }
 
        private void OnGUI()
        {
            Init();
            using (var scroll = new GUILayout.ScrollViewScope(_position))
            {
                _position = scroll.scrollPosition;
                foreach (var style in _editorGUIStyles)
                {
                    using (new EditorGUILayout.HorizontalScope("box"))
                    {
                        EditorGUILayout.SelectableLabel(style.name);
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField(style.name, style, GUILayout.ExpandWidth(true));
                    }
                }
            }
        }
    }

}