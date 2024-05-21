using UnityEngine;
using UnityEditor;

namespace JanSharp
{
    public static class CreateParent
    {
        [MenuItem("GameObject/Create Parent", isValidateFunction: true, priority = 0)]
        public static bool DoCreateParentValidation()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("GameObject/Create Parent", priority = 0)]
        public static void DoCreateParent(MenuCommand menuCommand)
        {
            // Prevent it from running multiple times.
            // Unity runs this only once when using the drop down, in this case context is null.
            // Unity runs this for every selected game object when using right click, so only run this if the
            // current call is for the active game object. Oddly enough this even works when the inspector is
            // locked onto an object which is not selected. Don't ask me how, but that makes this the least
            // hackly solution to this problem.
            if (menuCommand.context != null && menuCommand.context != Selection.activeGameObject)
                return;

            GameObject[] selected = Selection.gameObjects;
            // Doing validation here instead of the validation function because I do not want this validation
            // to run every time you right click in the hierarchy because this is O(n) where n is the amount
            // of selected objects.
            foreach (GameObject go in selected)
                if (PrefabUtility.IsPartOfAnyPrefab(go) && !PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                {
                    Debug.LogError("Cannot create parent for game objects which are part of prefabs.", go);
                    return;
                }

            GameObject parentGo = new GameObject();
            // I'd have loved to use the game object which was right clicked, however the menu item function
            // gets raised for each selected game object where the menuCommand.context changes each iteration.
            // This makes it impossible - to my knowledge - to know which object was actually right clicked.
            // Selection.activeGameObject appears to be consistent as the "first selected" game object. I'd
            // have preferred the last selected, however the order of objects in the selected array appears to
            // be unpredictable. So I guess this is the best we got.
            Transform parentParent = Selection.activeGameObject.transform.parent;
            if (parentParent != null)
                GameObjectUtility.SetParentAndAlign(parentGo, parentParent.gameObject);
            Transform parent = parentGo.transform;
            parent.SetSiblingIndex(Selection.activeGameObject.transform.GetSiblingIndex());
            Undo.RegisterCreatedObjectUndo(parentGo, "Create Parent");
            for (int i = selected.Length - 1; i >= 0; i--)
                Undo.SetTransformParent(selected[i].transform, parent, "Create Parent");
            Selection.activeObject = parentGo;
        }
    }
}
