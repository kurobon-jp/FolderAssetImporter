using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
#if ENABLE_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using UnityEngine;

namespace FolderAssetImporter
{
    [Serializable]
    public struct AddressNamingRule
    {
        [SerializeField] private string[] _includePatterns;
        [SerializeField] private string _group;
        [SerializeField] private string _address;
        [SerializeField] private string[] _labels;

        private static readonly Dictionary<string, int> _applyFrames = new();

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(_group) || !string.IsNullOrEmpty(_address);
        }

        public void Apply(string assetPath, bool isDryRun)
        {
            var now = Time.frameCount;
            if (_applyFrames.TryGetValue(assetPath, out var frameCount) && frameCount == now) return;
            _applyFrames[assetPath] = now;
            if (!IsMatch(assetPath, out var collection)) return;

#if ENABLE_ADDRESSABLES
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                throw new Exception("AddressableAssetSettings not found");
            }

            var group = settings.FindGroup(_group);
            if (group == null && !isDryRun)
            {
                var groupTemplate = settings.GetGroupTemplateObject(0) as AddressableAssetGroupTemplate;
                group = settings.CreateGroup(_group, false, false, true, null,
                    groupTemplate.GetTypes());
                groupTemplate.ApplyToAddressableAssetGroup(group);
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var address = _address;
            var labels = new List<string>();
            if (collection is { Count: > 1 })
            {
                var args = new object[collection.Count - 1];
                for (var i = 1; i < collection.Count; i++)
                {
                    args[i - 1] = collection[i].Value;
                }

                address = string.Format(address, args);
                foreach (var label in _labels)
                {
                    if (!string.IsNullOrEmpty(label))
                    {
                        labels.Add(string.Format(label, args));
                    }
                }
            }
            
            Debug.Log($"Applying address naming to {assetPath}\nGroup: {_group} Address: {address}");
            if (isDryRun) return;

            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.SetAddress(address);
            foreach (var label in entry.labels)
            {
                entry.SetLabel(label, false);
            }

            foreach (var label in labels)
            {
                entry.SetLabel(label, true, true);
            }
#endif
        }

        private bool IsMatch(string assetPath, out GroupCollection collection)
        {
            collection = null;
            if (string.IsNullOrEmpty(assetPath)) return false;
            if (_includePatterns == null || _includePatterns.Length == 0) return true;
            foreach (var pattern in _includePatterns)
            {
                var regex = new Regex(pattern);
                var match = regex.Match(assetPath);
                if (!match.Success) continue;
                collection = match.Groups;
                return true;
            }

            return false;
        }
    }
}