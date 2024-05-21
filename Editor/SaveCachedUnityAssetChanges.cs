using UnityEngine;
using UnityEditor;

namespace JanSharp
{
    public static class SaveCachedUnityAssetChanges
    {
        [MenuItem("Tools/JanSharp/Save Cached Unity Asset Changes", priority = 10000)]
        public static void DoSaveCachedUnityAssetChanges()
        {
            AssetDatabase.SaveAssets();
            Debug.Log("Saved Cached Unity Asset Changes!");
        }
    }
}
