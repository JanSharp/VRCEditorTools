using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;

namespace JanSharp
{
    public static class VisibilityMenuItems
    {
        [MenuItem("Tools/JanSharp/Show Selected Only", false, 1001)]
        public static void ShowSelectedOnly()
        {
            SceneVisibilityManager.instance.HideAll();
            foreach (GameObject go in Selection.gameObjects)
                SceneVisibilityManager.instance.Show(go, false);
        }

        [MenuItem("Tools/JanSharp/Show Non Selected Only", false, 1002)]
        public static void ShowNonSelectedOnly()
        {
            SceneVisibilityManager.instance.ShowAll();
            foreach (GameObject go in Selection.gameObjects)
                SceneVisibilityManager.instance.Hide(go, false);
        }

        [MenuItem("Tools/JanSharp/Show All", false, 1003)]
        public static void ShowAll()
        {
            SceneVisibilityManager.instance.ShowAll();
        }

        [MenuItem("Tools/JanSharp/Show None", false, 1004)]
        public static void ShowNone()
        {
            SceneVisibilityManager.instance.HideAll();
        }
    }
}
