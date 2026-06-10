using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace FolderAssetImporter
{
    [Serializable]
    public struct AssetPresettingRule
    {
        [SerializeField] private string[] _includePatterns;
        [SerializeField] private Preset[] _presets;

        public bool IsValid()
        {
            return _presets is { Length: > 0 };
        }

        public void Apply(string assetPath, AssetImporter importer, bool isDryRun)
        {
            var count = 0;
            foreach (var pattern in _includePatterns)
            {
                if (!Regex.IsMatch(assetPath, pattern)) continue;
                count++;
                break;
            }

            if (count == 0) return;
            foreach (var preset in _presets)
            {
                if (preset != null)
                {
                    if (!isDryRun)
                    {
                        preset.ApplyTo(importer);
                    }

                    Debug.Log($"Applying preset {preset.name} to {assetPath}");
                }
            }
        }
    }
}