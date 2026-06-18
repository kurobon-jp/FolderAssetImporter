using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEngine;

namespace FolderAssetImporter
{
    internal class FolderAssetProcessor : AssetPostprocessor
    {
        public override int GetPostprocessOrder()
        {
            return int.MaxValue;
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            for (var i = 0; i < movedAssets.Length; i++)
            {
                if (!Directory.Exists(movedAssets[i])) continue;
                FolderAssetImportSettings.Instance.RemoveCache(movedFromAssetPaths[i]);
            }

            var reimportAssets = new HashSet<string>();
            foreach (var assetPath in importedAssets)
            {
                if (Directory.Exists(assetPath)) continue;
                FolderAssetImportSettings.Instance.Import(assetPath);
                reimportAssets.Add(assetPath);
            }

            foreach (var assetPath in movedAssets)
            {
                if (Directory.Exists(assetPath)) continue;
                FolderAssetImportSettings.Instance.Import(assetPath);
                reimportAssets.Add(assetPath);
            }

            if (reimportAssets.Count == 0) return;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var assetPath in reimportAssets)
                {
                    var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                    if (EditorUtility.IsDirty(asset))
                    {
                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.Default);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}