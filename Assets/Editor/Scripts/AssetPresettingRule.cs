using System;
using System.Collections.Generic;
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

        private static readonly Dictionary<string, int> _applyFrames = new();

        public bool IsValid()
        {
            return _presets is { Length: > 0 };
        }

        public void Apply(string assetPath, bool isDryRun)
        {
            var now = Time.frameCount;
            if (_applyFrames.TryGetValue(assetPath, out var frameCount) && frameCount == now) return;
            _applyFrames[assetPath] = now;

            var count = 0;
            foreach (var pattern in _includePatterns)
            {
                if (!Regex.IsMatch(assetPath, pattern)) continue;
                count++;
                break;
            }

            if (count == 0) return;
            Presetting(assetPath, isDryRun);
        }

        private void Presetting(string assetPath, bool isDryRun)
        {
            foreach (var preset in _presets)
            {
                if (preset != null)
                {
                    Debug.Log($"Applying preset {preset.name} to {assetPath}");
                }
            }

            if (isDryRun) return;
            var importer = AssetImporter.GetAtPath(assetPath);
            foreach (var preset in _presets)
            {
                if (preset != null)
                {
                    preset.ApplyTo(importer);
                }
            }

            importer.SaveAndReimport();
        }
    }
}