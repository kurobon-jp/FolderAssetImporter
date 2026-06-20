using System.Collections.Generic;
using UnityEditor;
using System.IO;

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
                if (FolderAssetImportSettings.Instance.Import(assetPath))
                {
                    reimportAssets.Add(assetPath);
                }
            }

            foreach (var assetPath in movedAssets)
            {
                if (Directory.Exists(assetPath)) continue;
                if (FolderAssetImportSettings.Instance.Import(assetPath))
                {
                    reimportAssets.Add(assetPath);
                }
            }

            if (reimportAssets.Count == 0) return;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var assetPath in reimportAssets)
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.Default);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}