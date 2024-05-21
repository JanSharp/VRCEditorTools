using UnityEngine;
using UnityEditor;

namespace JanSharp
{
    public static class BulkLODGroups
    {
        private static bool hasCopiedValues = false;
        private static float cullPercentage = 0f;

        [MenuItem("CONTEXT/LODGroup/Copy Cull Percentage", isValidateFunction: true)]
        public static bool ValidateCopyCullPercentage(MenuCommand menuCommand)
        {
            LODGroup group = (LODGroup)menuCommand.context;
            return Selection.gameObjects.Length == 1
                && group.lodCount != 0
                && group.GetLODs()[group.lodCount - 1].screenRelativeTransitionHeight != 0f;
        }

        [MenuItem("CONTEXT/LODGroup/Copy Cull Percentage")]
        public static void CopyCullPercentage(MenuCommand menuCommand)
        {
            LODGroup group = (LODGroup)menuCommand.context;
            cullPercentage = group.GetLODs()[group.lodCount - 1].screenRelativeTransitionHeight;
            hasCopiedValues = true;
        }

        [MenuItem("CONTEXT/LODGroup/Paste Cull Percentage", isValidateFunction: true)]
        public static bool ValidatePasteCullPercentage()
        {
            return hasCopiedValues;
        }

        [MenuItem("CONTEXT/LODGroup/Paste Cull Percentage")]
        public static void PasteCullPercentage(MenuCommand menuCommand)
        {
            LODGroup group = (LODGroup)menuCommand.context;
            if (group.lodCount == 0)
            {
                Debug.LogError($"Could not paste cull percentage to '{group.name}' because the LOD Group has 0 LODs.", group);
                return;
            }
            LOD[] LODs = group.GetLODs();
            if (group.lodCount > 1 && LODs[group.lodCount - 2].screenRelativeTransitionHeight <= cullPercentage)
            {
                Debug.LogError($"Could not paste cull percentage to '{group.name}' because "
                    + "the second last LOD in the group would overlap.", group);
                return;
            }
            SerializedObject proxy = new SerializedObject(group);
            proxy.FindProperty("m_LODs")
                .GetArrayElementAtIndex(group.lodCount - 1)
                .FindPropertyRelative("screenRelativeHeight").floatValue = cullPercentage;
            proxy.ApplyModifiedProperties();
        }

        [MenuItem("Tools/JanSharp/Generate Basic Culling LOD", isValidateFunction: true, priority = 1000)]
        public static bool ValidateGenerateBasicCullingLOD()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("Tools/JanSharp/Generate Basic Culling LOD", priority = 1000)]
        public static void GenerateBasicCullingLOD()
        {
            int generatedCount = 0;
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go.GetComponent<LODGroup>() != null)
                {
                    Debug.LogError($"Won't generate LOD Group for {go.name} because it already has one.", go);
                    continue;
                }
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    Debug.LogError($"Won't generate LOD Group for {go.name} because it has no renderers.", go);
                    continue;
                }
                LODGroup group = Undo.AddComponent<LODGroup>(go);
                group.SetLODs(new LOD[] { new LOD(0.04f, renderers) });
                generatedCount++;
            }
            Debug.Log($"Generated LOD Groups for {generatedCount} objects.");
        }
    }
}
