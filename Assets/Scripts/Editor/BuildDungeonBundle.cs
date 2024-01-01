using UnityEditor;
using System.IO;

public class BuildDungeonBundle
{
    [MenuItem("Lethal Dungeon/Build Dungeon Bundle")]
    static void BuildAllAssetBundles()
    {
        string assetBundleDirectory = "Assets/DungeonBundles";
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        BuildPipeline.BuildAssetBundles(assetBundleDirectory,
                                        BuildAssetBundleOptions.None,
                                        BuildTarget.StandaloneWindows);
    }
}
