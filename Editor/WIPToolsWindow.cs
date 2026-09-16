using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JanSharp
{
    public class WIPToolsWindow : EditorWindow
    {
        private VisualElement root;
        private SelectionStageWindow selectionStage;

        [MenuItem("Tools/JanSharp/WIP Tools Window", priority = 500)]
        public static void ShowWIPToolsWindow()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<WIPToolsWindow>();
            wnd.titleContent = new GUIContent("WIP Tools");
        }

        private void SearchIntoSelectionStage(ICollection<Object> results)
        {
            if (selectionStage == null)
            {
                selectionStage = CreateWindow<SelectionStageWindow>(typeof(WIPToolsWindow));
                selectionStage.titleContent = new GUIContent("Results Stage");
            }
            selectionStage.SetStage(results);
            selectionStage.Show();
            selectionStage.Focus();
        }

        private void CreateFindPrefabInstancesGUI()
        {
            Box box = new Box();
            Foldout foldout = new Foldout() { text = "Find Prefab Instances", value = false };
            ObjectField prefabObjField = new ObjectField("Prefab to Find")
            {
                allowSceneObjects = false,
                objectType = typeof(GameObject),
            };
            foldout.Add(prefabObjField);
            foldout.Add(new Button(() =>
            {
                if (prefabObjField.value == null)
                    return;
                string assetPath = AssetDatabase.GetAssetPath(prefabObjField.value);
                if (assetPath == null)
                    return;
                GameObject[] objs = FindPrefabInstances(assetPath);
                SearchIntoSelectionStage(objs);
            })
            { text = "Search into Selection Stage" });
            box.Add(foldout);
            root.Add(box);
        }

        private static GameObject[] FindPrefabInstances(string prefabAssetPath)
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Transform>(includeInactive: true))
                .Select(t => t.gameObject)
                .Where(go => PrefabUtility.IsAnyPrefabInstanceRoot(go)
                    && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == prefabAssetPath)
                .ToArray();
        }

        private static GameObject[] FindPrefabInstances(GUID prefabAssetGUID)
            => FindPrefabInstances(AssetDatabase.GUIDToAssetPath(prefabAssetGUID));

        private void CreateFindMaterialsUsingATextureGUI()
        {
            Box box = new Box();
            Foldout foldout = new Foldout() { text = "Find Materials using given Texture", value = false };
            ObjectField textureObjField = new ObjectField("Texture to Find")
            {
                allowSceneObjects = false,
                objectType = typeof(Texture),
            };
            foldout.Add(textureObjField);
            foldout.Add(new Button(() =>
            {
                List<Object> foundMaterials = new List<Object>();
                Texture textureToFind = (Texture)textureObjField.value;
                if (textureToFind == null)
                    return;
                string[] guids = AssetDatabase.FindAssets("t:material");
                foreach (string guid in guids)
                {
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    foreach (string propName in material.GetPropertyNames(MaterialPropertyType.Texture))
                        if (material.GetTexture(propName) == textureToFind)
                        {
                            foundMaterials.Add(material);
                            break;
                        }
                }
                SearchIntoSelectionStage(foundMaterials);
            })
            { text = "Search into Selection Stage" });
            box.Add(foldout);
            root.Add(box);
        }

        private void CreateGenerateHiddenChangesConfGUI()
        {
            Box box = new Box();
            Foldout foldout = new Foldout() { text = "Generate Hidden Changes Conf", value = false };

            foldout.Add(
                new Label("Overwrites the hidden_changes.conf file in the root of the project!\n"
                    + "Custom content past the line '# Custom' will be kept.\n"
                    + "This adds all UdonSharp asset files to the list of hidden changes because it is "
                    + "nonsensical for those changes to be part of version control.")
                { style = { whiteSpace = WhiteSpace.Normal } });

            foldout.Add(new Button(() =>
            {
                const string CustomContentHeader = "\n# Custom\n";
                string customContent = CustomContentHeader
                    + "# Anything added below will not be overwritten by the script\n"
                    + "# which generates the list of all UdonSharp asset files above.\n";
                if (File.Exists("hidden_changes.conf"))
                {
                    customContent = File.ReadAllText("hidden_changes.conf");
                    int index = customContent.IndexOf(CustomContentHeader);
                    if (index != -1)
                        customContent = customContent.Substring(index);
                }

                StringBuilder sb = new();
                sb.Append('\n'); // Platform independent, unlike AppendLine, good for source control.
                sb.Append("# UdonSharp asset files\n");
                sb.Append("#\n");
                sb.Append("# This list is generated, do not modify it manually.");
                sb.Append("#\n");
                sb.Append("# Their existence is not auto generated, neither is their name nor script reference.\n");
                sb.Append("# However the rest of the content is auto generated and is irrelevant for source control.\n");
                sb.Append("# Hiding their changes prevents needless changes getting checked in and causing conflicts.\n");
                sb.Append("#\n");
                int ignoredFileCount = 0;
                foreach (string guid in AssetDatabase.FindAssets("t:UdonSharpProgramAsset"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid); // Always uses forward slashes, yay!
                    if (!File.Exists(path)) // Part of something weird, can safely ignore.
                        continue;
                    sb.Append('/'); // Leading slash to root it at the project root, pretty sure.
                    sb.Append(path);
                    sb.Append('\n');
                    ignoredFileCount++;
                }
                sb.Append("# End of generated list of UdonSharp asset files\n");
                sb.Append(customContent);
                File.WriteAllText("hidden_changes.conf", sb.ToString());

                Debug.Log($"Generated hidden_changes.conf file for {ignoredFileCount} UdonSharp asset files.");
            })
            { text = "Generate" });

            box.Add(foldout);
            root.Add(box);
        }

        private void CreateReplaceMeshesWithPrefabsFromFolderGUI()
        {
            Box box = new Box();
            Foldout foldout = new Foldout() { text = "Replace Meshes With Prefabs From Folder", value = false };

            foldout.Add(
                new Label("Builds a lookup table from all meshes used by mesh filters for each given prefab in the given folder recursively.\n"
                    + "Then goes through all mesh filters in the scene, checks if they are not part of a prefab instance, and if the "
                    + "mesh they are using exists in one of the given prefabs, that object in the scene will get replaced with "
                    + "the prefab.\n"
                    + "It makes sure to walk up in the hierarchy, which is to say the mesh filters can be children within each prefab.")
                { style = { whiteSpace = WhiteSpace.Normal } });

            TextField folderPathField = new TextField("Folder with Prefabs")
            {
                tooltip = "Full path relative to the root of the project, like Assets/Foo/Bar",
            };
            Toggle keepOriginalCountPostfixToggle = new Toggle("Keep Original (#) Postfix") { value = true };
            Toggle keepOriginalNameToggle = new Toggle("Keep Original Name");
            Toggle recordUndoToggle = new Toggle("Record Undo") { value = true };
            foldout.Add(folderPathField);
            foldout.Add(keepOriginalCountPostfixToggle);
            foldout.Add(keepOriginalNameToggle);
            foldout.Add(recordUndoToggle);

            foldout.Add(new Button(() =>
            {
                if (!Directory.Exists(folderPathField.text))
                    return;

                Dictionary<Mesh, (GameObject prefab, int hierarchyDepth)> meshesToPrefabsLut = new();
                HashSet<Mesh> reusedMeshes = new();

                int GetHierarchyDepth(Transform t)
                {
                    int depth = 0;
                    while (t.parent != null)
                    {
                        depth++;
                        t = t.parent;
                    }
                    return depth;
                }

                void RegisterMesh(MeshFilter meshFilter, GameObject prefab)
                {
                    Mesh mesh = meshFilter.sharedMesh;
                    if (mesh == null || reusedMeshes.Contains(mesh))
                        return;
                    if (meshesToPrefabsLut.Remove(mesh))
                    {
                        reusedMeshes.Add(mesh);
                        return;
                    }
                    meshesToPrefabsLut.Add(mesh, (prefab, GetHierarchyDepth(meshFilter.transform)));
                }

                // Build the lookup table.

                HashSet<GameObject> allPrefabs = new();

                void WalkDirectory(string dirPath)
                {
                    foreach (string subDirPath in Directory.EnumerateDirectories(dirPath))
                        WalkDirectory(subDirPath);
                    foreach (string filePath in Directory.EnumerateFiles(dirPath))
                    {
                        if (Path.GetExtension(filePath) != ".prefab")
                            continue;
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(filePath);
                        if (prefab == null)
                            continue;
                        allPrefabs.Add(prefab);
                        foreach (MeshFilter meshFilter in prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true))
                            RegisterMesh(meshFilter, prefab);
                    }
                }
                WalkDirectory(folderPathField.text);

                foreach (var registered in meshesToPrefabsLut.Values)
                    allPrefabs.Remove(registered.prefab);
                foreach (GameObject prefab in allPrefabs)
                    Debug.LogWarning($"Cannot uniquely identify the prefab {prefab.name} - {AssetDatabase.GetAssetPath(prefab)}", prefab);

                // Go through the scene.

                bool TryReplace(GameObject toReplace, GameObject prefab)
                {
                    GameObject to = (GameObject)PrefabUtility.InstantiatePrefab(prefab, toReplace.transform.parent);
                    if (to == null)
                        return false;
                    if (recordUndoToggle.value)
                        Undo.RegisterCreatedObjectUndo(to, $"replace object with '{prefab.name}'");
                    to.transform.SetSiblingIndex(toReplace.transform.GetSiblingIndex());
                    BulkReplaceWindow.ChangeName(toReplace, to, keepOriginalNameToggle.value, keepOriginalCountPostfixToggle.value);
                    to.transform.localPosition = toReplace.transform.localPosition;
                    to.transform.localRotation = toReplace.transform.localRotation;
                    to.transform.localScale = toReplace.transform.localScale;
                    if (recordUndoToggle.value)
                        Undo.DestroyObjectImmediate(toReplace);
                    else
                        DestroyImmediate(toReplace);
                    return true;
                }

                Transform GetNthParent(Transform t, int depth)
                {
                    for (int i = 0; i < depth; i++)
                    {
                        t = t.parent;
                        if (t == null)
                            return null;
                    }
                    return t;
                }

                int replacedCount = 0;

                foreach (MeshFilter meshFilter in FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (meshFilter == null // Objects get deleted (replaced) during the loop.
                        || meshFilter.sharedMesh == null
                        || PrefabUtility.IsPartOfPrefabInstance(meshFilter)
                        || !meshesToPrefabsLut.TryGetValue(meshFilter.sharedMesh, out (GameObject prefab, int hierarchyDepth) toReplaceWith))
                    {
                        continue;
                    }
                    Transform rootToReplace = GetNthParent(meshFilter.transform, toReplaceWith.hierarchyDepth);
                    if (rootToReplace == null)
                        continue;
                    if (TryReplace(rootToReplace.gameObject, toReplaceWith.prefab))
                        replacedCount++;
                }

                Debug.Log($"Replaced {replacedCount} objects with prefabs.");
            })
            { text = "Replace" });
            box.Add(foldout);
            root.Add(box);
        }

        private void AddVerticalSpacer(VisualElement parent)
        {
            parent.Add(new VisualElement() { style = { height = 4 } });
        }

        public void CreateGUI()
        {
            root = new ScrollView();
            CreateFindPrefabInstancesGUI();
            AddVerticalSpacer(root);
            CreateFindMaterialsUsingATextureGUI();
            AddVerticalSpacer(root);
            CreateGenerateHiddenChangesConfGUI();
            AddVerticalSpacer(root);
            CreateReplaceMeshesWithPrefabsFromFolderGUI();
            rootVisualElement.Add(root);
        }
    }
}
