using UnityEngine;
using UnityEditor;

namespace JanSharp
{
    public static class SavePendingAssetChanges
    {
        [MenuItem("Tools/Save Pending Asset Changes")]
        public static void DoSavePendingAssetChanges()
        {
            AssetDatabase.SaveAssets();
            Debug.Log("Saved pending asset changes!");
        }
    }
}
