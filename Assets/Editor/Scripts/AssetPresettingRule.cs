using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace FolderAssetImporter
{
    [Serializable]
    internal class AssetPresettingRule
    {
        [SerializeField] private string[] _includePatterns;
        [SerializeField] private Preset[] _presets;

        internal bool TryGetApplier(string assetPath, out Applier applier)
        {
            applier = null;
            if (_presets == null || _includePatterns == null || _presets.All(x => x == null)) return false;
            foreach (var pattern in _includePatterns)
            {
                if (!Regex.IsMatch(assetPath, pattern)) continue;
                applier = new Applier(assetPath, _presets);
                return true;
            }

            return false;
        }

        internal class Applier
        {
            private readonly string _assetPath;
            private readonly Preset[] _presets;
            
            internal Applier(string assetPath, Preset[] presets)
            {
                _assetPath = assetPath;
                _presets = presets;
            }
            
            internal void Log()
            {
                foreach (var preset in _presets)
                {
                    if (preset == null) continue;
                    Debug.Log($"Applying preset {preset.name} to {_assetPath}");
                }
            }

            internal void Apply()
            {
                var importer = AssetImporter.GetAtPath(_assetPath);
                foreach (var preset in _presets)
                {
                    if (preset == null) continue;
                    preset.ApplyTo(importer);
                }
            }
        }
    }
}