using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FolderAssetImporter
{
    [System.Serializable]
    public class FolderAssetImportSetting
    {
        private const string RootPath = "Assets";

        [SerializeField] private bool _enableAssetPresetting;
        [SerializeField] private List<AssetPresettingRule> _assetPresettingRules = new();
        [SerializeField] private bool _enableAddressNaming;
        [SerializeField] private List<AddressNamingRule> _addressNamingRules = new();

        private string ToJson()
        {
            return EditorJsonUtility.ToJson(this, false);
        }

        private static FolderAssetImportSetting FromJson(string json)
        {
            var setting = new FolderAssetImportSetting();
            EditorJsonUtility.FromJsonOverwrite(json, setting);
            return setting;
        }

        private static FolderAssetImportSetting FromPath(string folderPath)
        {
            var importer = AssetImporter.GetAtPath(folderPath);
            return FromJson(importer?.userData);
        }

        private static void AssetPresetting(string assetPath, bool isDryRun = false)
        {
            var parentPath = Path.GetDirectoryName(assetPath);
            while (!string.IsNullOrEmpty(parentPath) && RootPath != parentPath)
            {
                var setting = FromPath(parentPath);
                if (setting == null) continue;
                if (setting._enableAssetPresetting)
                {
                    foreach (var rule in setting._assetPresettingRules)
                    {
                        if (!rule.IsValid()) continue;
                        rule.Apply(assetPath, isDryRun);
                    }

                    return;
                }

                parentPath = Path.GetDirectoryName(parentPath);
            }
        }

        private static void AddressNaming(string assetPath, bool isDryRun = false)
        {
            var parentPath = Path.GetDirectoryName(assetPath);
            while (!string.IsNullOrEmpty(parentPath) && RootPath != parentPath)
            {
                var setting = FromPath(parentPath);
                if (setting == null) continue;
                if (setting._enableAddressNaming)
                {
                    foreach (var rule in setting._addressNamingRules)
                    {
                        if (!rule.IsValid()) continue;
                        rule.Apply(assetPath, isDryRun);
                    }

                    return;
                }

                parentPath = Path.GetDirectoryName(parentPath);
            }
        }

        public static void Import(string assetPath)
        {
            AssetPresetting(assetPath);
            AddressNaming(assetPath);
        }

        public class Wrapper : ScriptableObject
        {
            [SerializeField] private FolderAssetImportSetting _setting = new();

            private AssetImporter _importer;
            private string _pasteData;
            private bool _isChanged;

            public void Setup(AssetImporter importer)
            {
                _setting = FromJson(importer.userData);
                _importer = importer;
            }

            public void Save(bool force = false)
            {
                if (_isChanged || force)
                {
                    _isChanged = false;
                    _importer.userData = _setting.ToJson();
                    _importer.SaveAndReimport();
                }

                EditorUtility.SetDirty(this);
            }

            public void ReImport(bool isDryRun = false)
            {
                Save();
                var assetPath = _importer.assetPath;
                var files = Directory.EnumerateFileSystemEntries(assetPath, "*", SearchOption.AllDirectories)
                    .Where(x => !x.EndsWith(".meta") && !x.EndsWith("~") && !Path.GetFileName(x).StartsWith("."))
                    .Select(x => x.Replace("\\", "/"))
                    .OrderBy(f => f.Split("/").Length)
                    .ThenBy(f => f);
                string dir = null;
                var skipAssetPresetting = false;
                var skipAddressNaming = false;
                foreach (var file in files)
                {
                    if (Directory.Exists(file))
                    {
                        if (file != dir && (dir == null || !file.StartsWith(dir)))
                        {
                            dir = file;
                            var setting = FromPath(file);
                            if (setting != null)
                            {
                                skipAssetPresetting = setting._enableAssetPresetting;
                                skipAddressNaming = setting._enableAddressNaming;
                            }
                        }

                        continue;
                    }

                    if (!skipAssetPresetting)
                    {
                        AssetPresetting(file, isDryRun);
                    }

                    if (!skipAddressNaming)
                    {
                        AddressNaming(file, isDryRun);
                    }
                }
            }

            public void SetChanged()
            {
                _isChanged = true;
            }

            public void Clear()
            {
                SetUserData();
            }

            public void Copy()
            {
                _pasteData = _importer.userData;
            }

            public void Paste()
            {
                SetUserData(_pasteData);
            }

            public bool CanPaste()
            {
                return !string.IsNullOrEmpty(_pasteData);
            }

            private void SetUserData(string userData = null)
            {
                _setting = FromJson(userData);
                _importer.userData = userData;
                _importer.SaveAndReimport();
            }
        }
    }
}