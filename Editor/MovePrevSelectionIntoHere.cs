using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace JanSharp
{
    public static class MovePrevSelectionIntoHere
    {
        private static HashSet<GameObject> toMove = new();
        private static GameObject[] previousSelection = new GameObject[0];
        private static GameObject[] currentSelection = new GameObject[0];

        static MovePrevSelectionIntoHere()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            previousSelection = currentSelection;
            currentSelection = Selection.gameObjects;
        }

        private static HashSet<GameObject> GetToIgnoreLut()
        {
            HashSet<GameObject> toIgnore = new();
            Transform targetTransform = currentSelection[0].transform;
            while (targetTransform != null)
            {
                toIgnore.Add(targetTransform.gameObject);
                targetTransform = targetTransform.parent;
            }
            return toIgnore;
        }

        [MenuItem("GameObject/Move Prev Selection Into Here", isValidateFunction: true, priority = 0)]
        public static bool DoMovePrevSelectionIntoHereValidation()
        {
            if (previousSelection.Length == 0 || currentSelection.Length != 1)
                return false;
            HashSet<GameObject> toIgnore = GetToIgnoreLut();
            return previousSelection.Any(go => !toIgnore.Contains(go) && !SelectionStageWindow.IsAsset(go))
                && !previousSelection.Contains(currentSelection[0]);
        }

        [MenuItem("GameObject/Move Prev Selection Into Here", priority = 0)]
        public static void DoMovePrevSelectionIntoHere()
        {
            HashSet<GameObject> toIgnore = GetToIgnoreLut();
            List<Object> sorted = SelectionStageWindow.SortByHierarchy(previousSelection);
            toMove.Clear();
            Transform targetTransform = currentSelection[0].transform;
            foreach (Object obj in sorted)
                if (!toIgnore.Contains(obj) && !SelectionStageWindow.IsAsset(obj))
                    Undo.SetTransformParent(((GameObject)obj).transform, targetTransform, "Move Prev Selection Into Here");
        }
    }
}
