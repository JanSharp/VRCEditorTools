using UnityEngine;
using UnityEditor;

namespace JanSharp
{
    public static class SaveCachedUnityAssetChanges
    {
        [MenuItem("Tools/JanSharp/Save Cached Unity Asset Changes", false, 1000)]
        public static void DoSaveCachedUnityAssetChanges()
        {
            AssetDatabase.SaveAssets();
            Debug.Log("Saved Cached Unity Asset Changes!");
        }
    }
}
