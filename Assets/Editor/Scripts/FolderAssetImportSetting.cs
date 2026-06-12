using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FolderAssetImporter
{
    [Serializable]
    internal class FolderAssetImportSetting
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

        private static bool CollectAppliers(string assetPath, List<AssetPresettingRule.Applier> appliers)
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
                        if (rule.TryGetApplier(assetPath, out var applier))
                        {
                            appliers.Add(applier);
                        }
                    }

                    return appliers.Count > 0;
                }

                parentPath = Path.GetDirectoryName(parentPath);
            }

            return false;
        }

        private static bool CollectAppliers(string assetPath, List<AddressNamingRule.Applier> appliers)
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
                        if (rule.TryGetApplier(assetPath, out var applier))
                        {
                            appliers.Add(applier);
                        }
                    }

                    return appliers.Count > 0;
                }

                parentPath = Path.GetDirectoryName(parentPath);
            }

            return false;
        }

        internal static void Import(string assetPath)
        {
            var presettingAppliers = new List<AssetPresettingRule.Applier>();
            if (CollectAppliers(assetPath, presettingAppliers))
            {
                foreach (var applier in presettingAppliers)
                {
                    applier.Apply();
                    applier.Log();
                }
            }
#if ENABLE_ADDRESSABLES
            var addressNamingAppliers = new List<AddressNamingRule.Applier>();
            if (CollectAppliers(assetPath, addressNamingAppliers))
            {
                foreach (var applier in addressNamingAppliers)
                {
                    applier.Apply();
                    applier.Log();
                }
            }
#endif
        }

        internal class Wrapper : ScriptableObject
        {
            [SerializeField] private FolderAssetImportSetting _setting = new();

            private AssetImporter _importer;
            private string _pasteData;
            private bool _isChanged;

            internal void Setup(AssetImporter importer)
            {
                _setting = FromJson(importer.userData);
                _importer = importer;
            }

            internal void Save(bool force = false)
            {
                if (_isChanged || force)
                {
                    _isChanged = false;
                    _importer.userData = _setting.ToJson();
                }
            }

            internal void ReImport(bool isDryRun = false)
            {
                Save();
                EditorUtility.SetDirty(this);
                AssetDatabase.StartAssetEditing();
                try
                {
                    var assetPath = _importer.assetPath;
                    var files = Directory.EnumerateFileSystemEntries(assetPath, "*", SearchOption.AllDirectories)
                        .Where(x => !x.EndsWith(".meta") && !x.EndsWith("~") && !Path.GetFileName(x).StartsWith("."))
                        .Select(x => x.Replace("\\", "/"))
                        .OrderBy(f => f.Split("/").Length)
                        .ThenBy(f => f);
                    string dir = null;
                    var skipAssetPresetting = false;
                    var skipAddressNaming = false;
                    var reimportAssets = new HashSet<string>();
                    var presettingAppliers = new List<AssetPresettingRule.Applier>();
                    var addressNamingAppliers = new List<AddressNamingRule.Applier>();
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

                        presettingAppliers.Clear();
                        if (!skipAssetPresetting && CollectAppliers(file, presettingAppliers))
                        {
                            foreach (var applier in presettingAppliers)
                            {
                                if (isDryRun)
                                {
                                    applier.Log();
                                }
                                else
                                {
                                    applier.Apply();
                                    reimportAssets.Add(file);
                                }
                            }
                        }
#if ENABLE_ADDRESSABLES
                        addressNamingAppliers.Clear();
                        if (!skipAddressNaming && CollectAppliers(file, addressNamingAppliers))
                        {
                            foreach (var applier in addressNamingAppliers)
                            {
                                if (isDryRun)
                                {
                                    applier.Log();
                                }
                                else
                                {
                                    applier.Apply();
                                    reimportAssets.Add(file);
                                }
                            }
                        }
#endif
                    }

                    foreach (var reimportAsset in reimportAssets)
                    {
                        AssetDatabase.ImportAsset(reimportAsset, ImportAssetOptions.Default);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }

            internal void SetChanged()
            {
                _isChanged = true;
            }

            internal void Clear()
            {
                SetUserData();
            }

            internal void Copy()
            {
                _pasteData = _importer.userData;
            }

            internal void Paste()
            {
                SetUserData(_pasteData);
            }

            internal bool CanPaste()
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