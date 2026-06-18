using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FolderAssetImporter
{
    public class FolderAssetImportSettings : ScriptableObject
    {
        private const string RootPath = "Assets";

        private static FolderAssetImportSettings _instance;

        internal static FolderAssetImportSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    var guids = AssetDatabase.FindAssets($"t:FolderAssetImportSettings");
                    if (guids.Length > 0)
                    {
                        _instance = AssetDatabase.LoadAssetByGUID<FolderAssetImportSettings>(new GUID(guids[0]));
                    }
                }

                if (_instance == null)
                {
                    _instance = CreateInstance<FolderAssetImportSettings>();
                    AssetDatabase.CreateAsset(_instance, $"Assets/FolderAssetImportSettings.asset");
                }

                return _instance;
            }
        }

        [SerializeField] private List<FolderAssetImportSetting> _settings = new();

        private readonly Dictionary<string, FolderAssetImportSetting> _pathCache = new();
        private FolderAssetImportSetting _selected;
        private string _clipboard;

        private bool TryGet(string holderPath, out FolderAssetImportSetting setting)
        {
            setting = null;
            if (_pathCache.TryGetValue(holderPath, out setting) && setting.Holder != null) return true;
            var holder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(holderPath);
            if (holder == null) return false;
            for (var i = 0; i < _settings.Count; i++)
            {
                setting = _settings[i];
                if (setting?.Holder != holder) continue;
                _pathCache[holderPath] = setting;
                return true;
            }

            return false;
        }

        private bool TryGetAppliers(string assetPath, List<AssetPresettingRule.Applier> appliers)
        {
            var parentPath = Path.GetDirectoryName(assetPath);
            while (!string.IsNullOrEmpty(parentPath) && RootPath != parentPath)
            {
                if (TryGet(parentPath, out var setting) && setting.CollectAppliers(assetPath, appliers))
                {
                    return true;
                }

                parentPath = Path.GetDirectoryName(parentPath);
            }

            return false;
        }

        private bool TryGetAppliers(string assetPath, List<AddressNamingRule.Applier> appliers)
        {
            var parentPath = Path.GetDirectoryName(assetPath);
            while (!string.IsNullOrEmpty(parentPath) && RootPath != parentPath)
            {
                if (TryGet(parentPath, out var setting) && setting.CollectAppliers(assetPath, appliers))
                {
                    return true;
                }

                parentPath = Path.GetDirectoryName(parentPath);
            }

            return false;
        }

        private void Cleanup()
        {
            _settings.RemoveAll(x =>
            {
                var isValid = x.IsValid();
                if (isValid || x.Holder == null) return !isValid;
                var assetPath = AssetDatabase.GetAssetPath(x.Holder);
                RemoveCache(assetPath);
                return true;
            });
        }

        internal int GetIndex(Object target)
        {
            var holderPath = AssetDatabase.GetAssetPath(target);
            if (TryGet(holderPath, out _selected)) return _settings.IndexOf(_selected);
            _selected = _settings.FirstOrDefault(x => x.Holder == target);
            if (_selected == null)
            {
                _selected = new FolderAssetImportSetting { Holder = target };
            }
            else
            {
                _settings.Add(_selected);
            }

            _pathCache[holderPath] = _selected;
            return _settings.Count - 1;
        }

        internal void RemoveCache(string holderPath)
        {
            _pathCache.Remove(holderPath);
        }

        internal void Save()
        {
            Cleanup();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        internal void ReImport(string holderPath, bool isDryRun = false)
        {
            var files = Directory.EnumerateFileSystemEntries(holderPath, "*", SearchOption.AllDirectories)
                .Where(x => !x.EndsWith(".meta") && !x.EndsWith("~") && !Path.GetFileName(x).StartsWith("."))
                .Select(x => x.Replace("\\", "/"))
                .OrderBy(f => f);
            var skipAssetPresetting = false;
            var skipAddressNaming = false;
            var reimportAssets = new HashSet<string>();
            var presettingAppliers = new List<AssetPresettingRule.Applier>();
            var addressNamingAppliers = new List<AddressNamingRule.Applier>();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var file in files)
                {
                    if (Directory.Exists(file))
                    {
                        if (TryGet(file, out var setting))
                        {
                            skipAssetPresetting |= setting.EnableAssetPresetting;
                            skipAddressNaming |= setting.EnableAddressNaming;
                        }

                        continue;
                    }

                    presettingAppliers.Clear();
                    if (!skipAssetPresetting && _selected.CollectAppliers(file, presettingAppliers))
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
                    if (!skipAddressNaming && _selected.CollectAppliers(file, addressNamingAppliers))
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

        internal void Import(string assetPath)
        {
            var presettingAppliers = new List<AssetPresettingRule.Applier>();
            if (TryGetAppliers(assetPath, presettingAppliers))
            {
                foreach (var applier in presettingAppliers)
                {
                    applier.Apply();
                    applier.Log();
                }
            }
#if ENABLE_ADDRESSABLES
            var addressNamingAppliers = new List<AddressNamingRule.Applier>();
            if (TryGetAppliers(assetPath, addressNamingAppliers))
            {
                foreach (var applier in addressNamingAppliers)
                {
                    applier.Apply();
                    applier.Log();
                }
            }
#endif
        }

        internal void Clear()
        {
            _selected.Clear();
        }

        internal void Copy()
        {
            _clipboard = EditorJsonUtility.ToJson(_selected, false);
        }

        internal void Paste()
        {
            var holder = _selected.Holder;
            EditorJsonUtility.FromJsonOverwrite(_clipboard, _selected);
            _selected.Holder = holder;
        }

        internal bool CanPaste()
        {
            return !string.IsNullOrEmpty(_clipboard);
        }
    }
}