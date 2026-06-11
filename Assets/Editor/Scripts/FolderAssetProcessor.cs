using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEngine;

namespace FolderAssetImporter
{
    public class FolderAssetProcessor : AssetPostprocessor
    {
        public override int GetPostprocessOrder()
        {
            return int.MaxValue;
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            var reimportAssets = new HashSet<string>();
            foreach (var assetPath in importedAssets)
            {
                if (Directory.Exists(assetPath)) continue;
                FolderAssetImportSetting.Import(assetPath);
                reimportAssets.Add(assetPath);
            }

            foreach (var assetPath in movedAssets)
            {
                if (Directory.Exists(assetPath)) continue;
                FolderAssetImportSetting.Import(assetPath);
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