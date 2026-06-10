using UnityEditor;
using System.IO;

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
            foreach (var assetPath in importedAssets)
            {
                if (Directory.Exists(assetPath)) continue;
                FolderAssetImportSetting.Import(assetPath);
            }

            foreach (var assetPath in movedAssets)
            {
                if (Directory.Exists(assetPath)) continue;
                FolderAssetImportSetting.Import(assetPath);
            }
        }
    }
}