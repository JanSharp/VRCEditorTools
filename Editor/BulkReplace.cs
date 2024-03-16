using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;
using System.Text.RegularExpressions;

namespace JanSharp
{
    public class BulkReplaceWindow : EditorWindow
    {
        GameObject prefab;
        [SerializeField] bool keepCountPostfix = true;
        [SerializeField] Vector3 localPositionOffset;
        [SerializeField] Vector3 localRotationOffset;
        [SerializeField] float localScaleMultiplier = 1f;
        [SerializeField] bool keepChildrenPostLocalShift = false;
        [SerializeField] Vector3 worldPositionOffset;
        [SerializeField] Vector3 worldRotationOffset;
        [SerializeField] bool keepChildrenPostWorldShift = false;

        [MenuItem("Tools/Bulk Replace")]
        public static void ShowBulkReplaceWindow()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<BulkReplaceWindow>();
            wnd.titleContent = new GUIContent("Bulk Replace");
        }

        private string GetKeepChildrenTooltip(string modificationType)
        {
            return "Try to keep them at their original world position after " + modificationType
                + " transformations have been applied to the new parent.\n"
                + "If the original object is a prefab instance, only extra added children are kept.";
        }

        private void OnGUI()
        {
            SerializedObject proxy = new SerializedObject(this);
            prefab = (GameObject)EditorGUILayout.ObjectField("New Prefab", prefab, typeof(GameObject), allowSceneObjects: false);
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("The following all get applied in order:");
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(proxy.FindProperty(nameof(keepCountPostfix)), new GUIContent("Keep (#) Postfix"));
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(proxy.FindProperty(nameof(localPositionOffset)));
            EditorGUILayout.PropertyField(proxy.FindProperty(nameof(localRotationOffset)));
            EditorGUILayout.PropertyField(proxy.FindProperty(nameof(localScaleMultiplier)));
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(proxy.FindProperty(nameof(keepChildrenPostLocalShift)),
                new GUIContent("Keep Children", GetKeepChildrenTooltip("local")));
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(proxy.FindProperty(nameof(worldPositionOffset)));
            EditorGUILayout.PropertyField(proxy.FindProperty(nameof(worldRotationOffset)));
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(proxy.FindProperty(nameof(keepChildrenPostWorldShift)),
                new GUIContent("Keep Children", GetKeepChildrenTooltip("all")));
            EditorGUILayout.Separator();
            proxy.ApplyModifiedProperties();

            if (keepChildrenPostLocalShift && keepChildrenPostWorldShift)
            {
                EditorGUILayout.LabelField("Keeping children twice does not make sense.");
                return;
            }
            GameObject[] selected = Selection.gameObjects;
            if (GUILayout.Button($"Replace {selected.Length} objects"))
                Replace(selected);
        }

        private void Replace(GameObject[] toReplace)
        {
            if (toReplace.Length == 0)
                return;
            int successCount = 0;
            Quaternion actualLocalRotationOffset = Quaternion.Euler(localRotationOffset);
            Quaternion actualWorldRotationOffset = Quaternion.Euler(worldRotationOffset);
            foreach (GameObject from in toReplace)
            {
                GameObject to = (GameObject)PrefabUtility.InstantiatePrefab(prefab, from.transform.parent);
                if (to == null)
                    continue;
                Undo.RegisterCreatedObjectUndo(to, $"replace object with '{prefab.name}'");
                to.transform.SetSiblingIndex(from.transform.GetSiblingIndex());
                if (keepCountPostfix)
                    CopyCountPostfix(from, to);
                to.transform.localPosition = from.transform.localPosition + localPositionOffset;
                to.transform.localRotation = from.transform.localRotation * actualLocalRotationOffset;
                to.transform.localScale = from.transform.localScale * localScaleMultiplier;
                if (keepChildrenPostLocalShift)
                    TryKeepAddedChildren(from, to);
                to.transform.position += worldPositionOffset;
                to.transform.rotation *= actualWorldRotationOffset;
                if (keepChildrenPostWorldShift)
                    TryKeepAddedChildren(from, to);
                Undo.DestroyObjectImmediate(from);
                successCount++;
            }
            // I can't find where this is actually shown so I can't test it.
            Undo.SetCurrentGroupName($"replace {successCount} objects with '{prefab.name}'");
            Undo.IncrementCurrentGroup(); // Pretty sure this line is pointless, but :shrug:.
        }

        private static Regex countPostfixRegex = new Regex(@" \(\d+\)$", RegexOptions.RightToLeft | RegexOptions.Compiled);
        private void CopyCountPostfix(GameObject from, GameObject to)
        {
            Match fromMatch = countPostfixRegex.Match(from.name);
            if (!fromMatch.Success)
                return;
            // Strip an existing count postfix if it exists.
            to.name = countPostfixRegex.Replace(to.name, "") + fromMatch.Value;
        }

        private void TryKeepAddedChildren(GameObject from, GameObject to)
        {
            bool isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(from);
            foreach (Transform child in from.transform)
                if (!isPrefab || PrefabUtility.IsAddedGameObjectOverride(child.gameObject))
                    Undo.SetTransformParent(child, to.transform, "moved child for replacement");
                    // child.SetParent(to.transform, worldPositionStays: true);
        }
    }
}
