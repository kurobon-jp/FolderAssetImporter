using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
#if ENABLE_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace FolderAssetImporter
{
    [Serializable]
    internal class AddressNamingRule
    {
        [SerializeField] private string[] _includePatterns;
        [SerializeField] private string _group;
        [SerializeField] private string _address;
        [SerializeField] private string[] _labels;

        internal bool TryGetApplier(string assetPath, Object holder, out Applier applier)
        {
            applier = null;
            if (string.IsNullOrEmpty(_group) || string.IsNullOrEmpty(_address)) return false;
            if (!IsMatch(assetPath, out var collection)) return false;

            var address = _address;
            var labels = Array.Empty<string>();
            if (collection is { Count: > 1 })
            {
                var args = new object[collection.Count - 1];
                for (var i = 1; i < collection.Count; i++)
                {
                    args[i - 1] = collection[i].Value;
                }

                address = string.Format(address, args);
                labels = _labels
                    .Select(label => string.Format(label, args))
                    .Where(label => !string.IsNullOrEmpty(label))
                    .Distinct()
                    .ToArray();
            }

            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"Invalid address {address}", holder);
                return false;
            }

            if (string.IsNullOrEmpty(_group))
            {
                Debug.LogError($"Invalid group {_group}", holder);
                return false;
            }

            applier = new Applier(assetPath, _group, address, labels, holder);
            return true;
        }

        private bool IsMatch(string assetPath, out GroupCollection collection)
        {
            collection = null;
            if (string.IsNullOrEmpty(assetPath)) return false;
            if (_includePatterns == null || _includePatterns.Length == 0) return false;
            foreach (var pattern in _includePatterns)
            {
                var match = Regex.Match(assetPath, pattern);
                if (!match.Success) continue;
                collection = match.Groups;
                return true;
            }

            return false;
        }

        internal class Applier
        {
            private readonly string _assetPath;
            private readonly string _group;
            private readonly string _address;
            private readonly string[] _labels;
            private readonly Object _context;

            internal Applier(string assetPath, string group, string address, string[] labels, Object context)
            {
                _assetPath = assetPath;
                _group = group;
                _address = address;
                _labels = labels;
                _context = context;
            }

            internal void Log()
            {
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, _context,
                    $"Applying address naming to {_assetPath}\n" +
                    $"Group: {_group.Replace("{", "{{").Replace("}", "}}")} " +
                    $"Address: {_address.Replace("{", "{{").Replace("}", "}}")} " +
                    $"Labels: [{string.Join(",", _labels)}]");
            }

            internal void Apply()
            {
#if ENABLE_ADDRESSABLES
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                {
                    throw new Exception("AddressableAssetSettings not found");
                }

                var group = settings.FindGroup(_group);
                if (group == null)
                {
                    var groupTemplate = settings.GetGroupTemplateObject(0) as AddressableAssetGroupTemplate;
                    group = settings.CreateGroup(_group, false, false, true, null,
                        groupTemplate.GetTypes());
                    groupTemplate.ApplyToAddressableAssetGroup(group);
                }

                var guid = AssetDatabase.AssetPathToGUID(_assetPath);
                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.SetAddress(_address);
                foreach (var label in entry.labels)
                {
                    entry.SetLabel(label, false);
                }

                foreach (var label in _labels)
                {
                    entry.SetLabel(label, true, true);
                }
#endif
            }
        }
    }
}