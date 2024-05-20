using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;
using System.Text.RegularExpressions;

namespace JanSharp
{
    public class SelectionStage : EditorWindow
    {
        private List<GameObject> staged;
        private HashSet<GameObject> stagedLut;
        private ListView listView;
        private List<object> listViewSelected;
        private IEnumerable<GameObject> SelectedGameObjects => listViewSelected.Where(obj => obj != null).Cast<GameObject>();

        [MenuItem("Tools/JanSharp/Selection Stage", false, 980)]
        public static void ShowSelectionStage()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow window = CreateInstance<SelectionStage>();
            window.titleContent = new GUIContent("Selection Stage");
            window.Show();
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            staged = new List<GameObject>();
            stagedLut = new HashSet<GameObject>();
            listViewSelected = new List<object>(); // Just to make it never be null.

            System.Func<VisualElement> makeItem = () => {
                return new Label();
            };
            System.Action<VisualElement, int> bindItem = (element, index) => {
                Label label = (Label)element;
                label.text = staged[index].name;
            };
            listView = new ListView(staged, 16, makeItem, bindItem);
            listView.style.flexGrow = 1.0f;
            listView.selectionType = SelectionType.Multiple;

            listView.onItemChosen += obj => {
                GameObject go = (GameObject)obj;
                EditorGUIUtility.PingObject(go); // Jump to in hierarchy, without selecting.

                var prev = Selection.objects;
                Selection.activeObject = go;
                SceneView.FrameLastActiveSceneView();
                Selection.objects = prev;
            };

            listView.onSelectionChanged += selected => listViewSelected = selected;

            root.Add(listView);

            root.Add(new Button(AddToStage) { text = "Add to Stage" });
            root.Add(new Button(ExcludeFromStage) { text = "Exclude from Stage" });
            root.Add(new Button(RemoveFromStage) { text = "Remove from Stage" });
            root.Add(new Button(ClearStage) { text = "Clear Stage" });
            root.Add(new Button(ApplySelected) { text = "Apply selected" });
            root.Add(new Button(OverwriteSelected) { text = "Overwrite selected" });
        }

        private void RemoveObjectsFromStage(HashSet<GameObject> toRemove)
        {
            if (toRemove.Count == 0)
                return;
            int c = staged.Count;
            int newI = 0;
            for (int i = 0; i < c; i++)
            {
                GameObject go = staged[i];
                if (toRemove.Contains(go))
                    stagedLut.Remove(go);
                else
                    staged[newI++] = go;
            }
            staged.RemoveRange(newI, c - newI);
            listView.selectedIndex = -1;
            listView.Refresh();
        }

        private void Cleanup()
        {
            RemoveObjectsFromStage(new HashSet<GameObject>(staged.Where(go => go == null)));
        }

        private void OnHierarchyChange()
        {
            Cleanup();
        }

        private void AddToStage()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                if (stagedLut.Contains(go))
                    continue;
                stagedLut.Add(go);
                staged.Add(go);
            }
            listView.Refresh();
        }

        private void ExcludeFromStage()
        {
            RemoveObjectsFromStage(new HashSet<GameObject>(Selection.gameObjects));
        }

        private void RemoveFromStage()
        {
            RemoveObjectsFromStage(new HashSet<GameObject>(SelectedGameObjects));
        }

        private void ClearStage()
        {
            staged.Clear();
            stagedLut.Clear();
            listView.Refresh();
        }

        private void ApplySelected()
        {
            Selection.objects = Selection.gameObjects.Union(SelectedGameObjects).ToArray();
        }

        private void OverwriteSelected()
        {
            Selection.objects = SelectedGameObjects.ToArray();
        }
    }
}
