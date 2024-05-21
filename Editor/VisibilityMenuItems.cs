using UnityEngine;
using UnityEditor;
using System.Linq;

namespace JanSharp
{
    public static class VisibilityMenuItems
    {
        [MenuItem("Tools/JanSharp/Show Selected Only", isValidateFunction: true, priority = 1100)]
        public static bool ValidateShowSelectedOnly()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("Tools/JanSharp/Show Selected Only", priority = 1100)]
        public static void ShowSelectedOnly()
        {
            SceneVisibilityManager.instance.HideAll();
            foreach (GameObject go in Selection.gameObjects)
                SceneVisibilityManager.instance.Show(go, false);
        }

        [MenuItem("Tools/JanSharp/Show Non Selected Only", isValidateFunction: true, priority = 1101)]
        public static bool ValidateShowNonSelectedOnly()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("Tools/JanSharp/Show Non Selected Only", priority = 1101)]
        public static void ShowNonSelectedOnly()
        {
            SceneVisibilityManager.instance.ShowAll();
            foreach (GameObject go in Selection.gameObjects)
                SceneVisibilityManager.instance.Hide(go, false);
        }

        [MenuItem("Tools/JanSharp/Show All", priority = 1102)]
        public static void ShowAll()
        {
            SceneVisibilityManager.instance.ShowAll();
        }

        [MenuItem("Tools/JanSharp/Show None", priority = 1103)]
        public static void ShowNone()
        {
            SceneVisibilityManager.instance.HideAll();
        }

        [MenuItem("Tools/JanSharp/Invert Shown", priority = 1104)]
        public static void ShowOpposite()
        {
            void Walk(Transform parent)
            {
                SceneVisibilityManager.instance.ToggleVisibility(parent.gameObject, false);
                foreach (Transform child in parent)
                    Walk(child);
            }
            foreach (Transform root in UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().GetRootGameObjects().Select(go => go.transform))
                Walk(root);
        }
    }
}
