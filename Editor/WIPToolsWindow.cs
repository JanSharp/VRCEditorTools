using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.SceneManagement;

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
            rootVisualElement.Add(root);
        }
    }
}
