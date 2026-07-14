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
            rootVisualElement.Add(root);
        }
    }
}
