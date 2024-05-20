using UnityEngine;
using UnityEditor;

namespace JanSharp
{
    public static class SavePendingAssetChanges
    {
        [MenuItem("Tools/JanSharp/Save Pending Asset Changes", false, 1000)]
        public static void DoSavePendingAssetChanges()
        {
            AssetDatabase.SaveAssets();
            Debug.Log("Saved pending asset changes!");
        }
    }
}
