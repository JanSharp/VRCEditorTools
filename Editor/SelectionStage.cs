using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;

// TODO: Undo support for changes within the selection stage!
// TODO: Figure out how tooltips work.
// TODO: Once upgrading to 2022 add more and better selection support within the stage.

namespace JanSharp
{
    public class SelectionStage : EditorWindow
    {
        private List<GameObject> staged;
        private HashSet<GameObject> stagedLut;
        private ListView listView;
        private List<object> listViewSelected;
        private IEnumerable<GameObject> SelectedInStage => listViewSelected.Any(obj => obj != null)
            ? listViewSelected.Where(obj => obj != null).Cast<GameObject>()
            : staged;

        [MenuItem("Tools/JanSharp/Selection Stage", priority = 980)]
        public static void ShowSelectionStage()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow window = CreateInstance<SelectionStage>();
            window.titleContent = new GUIContent("Selection Stage");
            window.Show();
        }

        private void CreateGUI()
        {
            // TODO: don't reset here because every recompile causes the stage to get cleared because of this.
            staged = new List<GameObject>();
            stagedLut = new HashSet<GameObject>();
            listViewSelected = new List<object>(); // Just to make it never be null.

            VisualElement root = rootVisualElement;

            Box listBox = new Box() { style = { flexGrow = 1f } };

            System.Func<VisualElement> makeItem = () => new Label();
            System.Action<VisualElement, int> bindItem = (element, index) => {
                Label label = (Label)element;
                label.text = staged[index].name;
            };
            listView = new ListView(staged, 16, makeItem, bindItem)
            {
                style = { flexGrow = 1f },
                selectionType = SelectionType.Multiple,
            };
            listView.onItemChosen += obj => {
                GameObject go = (GameObject)obj;
                EditorGUIUtility.PingObject(go); // Jump to in hierarchy, without selecting.
                var prev = Selection.objects;
                Selection.activeObject = go;
                SceneView.FrameLastActiveSceneView();
                Selection.objects = prev;
                if (listViewSelected.Count == 1 && ((GameObject)listViewSelected[0]) == go)
                {
                    // When double clicking a single element, clear the selection again as to prevent
                    // accidentally narrowing the stage through having only 1 element selected.
                    listView.selectedIndex = -1;
                    listView.Refresh();
                }
            };
            listView.onSelectionChanged += selected => listViewSelected = selected;
            listBox.Add(listView);

            root.Add(listBox);

            Box buttonsBox = new Box();
            IMGUIContainer buttonColumns = new IMGUIContainer() { style = { flexDirection = FlexDirection.Row } };

            {
                IMGUIContainer column = new IMGUIContainer() { style = { flexGrow = 1f } };
                column.Add(new Label("Affect Stage")
                {
                    style = { alignSelf = Align.Center },
                    tooltip = "How should the current selection affect the stage?",
                });
                column.Add(new Button(AddToStage) { text = "Add" });
                column.Add(new Button(ExcludeFromStage) { text = "Exclude" });
                column.Add(new Button(FlipExcludeFromStage) { text = "Flip Exclude" });
                column.Add(new Button(IntersectWithStage) { text = "Intersect" });
                column.Add(new Button(SymmetricDifferenceWithStage) { text = "Symmetric Diff" });
                column.Add(new Button(OverwriteStage) { text = "Overwrite" });
                buttonColumns.Add(column);
            }

            {
                IMGUIContainer column = new IMGUIContainer() { style = { flexGrow = 1f } };
                column.Add(new Label("Affect Selection")
                {
                    style = { alignSelf = Align.Center },
                    tooltip = "How should the current stage affect the Selection?\n"
                        + "When nothing is selected within the stage, the entire stage is considered.",
                });
                column.Add(new Button(AddToSelection) { text = "Add", });
                column.Add(new Button(ExcludeFromSelection) { text = "Exclude" });
                column.Add(new Button(FlipExcludeFromSelection) { text = "Flip Exclude" });
                column.Add(new Button(IntersectWithSelection) { text = "Intersect" });
                column.Add(new Button(SymmetricDifferenceWithSelection) { text = "Symmetric Diff" });
                column.Add(new Button(OverwriteSelection) { text = "Overwrite" });
                buttonColumns.Add(column);
            }

            {
                IMGUIContainer column = new IMGUIContainer() { style = { flexGrow = 1f } };
                column.Add(new Label("Within Stage") { style = { alignSelf = Align.Center } });
                column.Add(new Button(DeselectWithinStage) { text = "Deselect" });
                column.Add(new Button(RemoveWithinStage) { text = "Remove" });
                column.Add(new Button(ClearStage) { text = "Clear" });
                buttonColumns.Add(column);
            }

            buttonsBox.Add(buttonColumns);
            root.Add(buttonsBox);
        }

        private void RemoveObjectsFromStage(IEnumerable<GameObject> toRemove, bool inverted = false)
        {
            if (!inverted && !toRemove.Any())
                return;
            int c = staged.Count;
            int newI = 0;
            for (int i = 0; i < c; i++)
            {
                GameObject go = staged[i];
                if (toRemove.Contains(go) != inverted)
                    stagedLut.Remove(go);
                else
                    staged[newI++] = go;
            }
            staged.RemoveRange(newI, c - newI);
            listView.selectedIndex = -1;
            listView.Refresh();
        }

        private void OverwriteStageEntirely(ICollection<GameObject> newSelection)
        {
            staged.Clear();
            staged.AddRange(newSelection);
            stagedLut = new HashSet<GameObject>(staged);
            listView.selectedIndex = -1;
            listView.Refresh();
        }

        private void Cleanup()
        {
            RemoveObjectsFromStage(staged.Where(go => go == null));
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
            RemoveObjectsFromStage(Selection.gameObjects);
        }

        private void FlipExcludeFromStage()
        {
            OverwriteStageEntirely(Selection.gameObjects.Except(SelectedInStage).ToList());
        }

        private void IntersectWithStage()
        {
            RemoveObjectsFromStage(Selection.gameObjects, inverted: true);
        }

        private void SymmetricDifferenceWithStage()
        {
            GameObject[] selected = Selection.gameObjects;
            OverwriteStageEntirely(SelectedInStage
                .Union(selected)
                .Except(SelectedInStage.Intersect(selected))
                .ToList());
        }

        private void OverwriteStage()
        {
            OverwriteStageEntirely(Selection.gameObjects);
        }


        private void AddToSelection()
        {
            Selection.objects = Selection.gameObjects.Union(SelectedInStage).ToArray();
        }

        private void ExcludeFromSelection()
        {
            Selection.objects = Selection.gameObjects.Except(SelectedInStage).ToArray();
        }

        private void FlipExcludeFromSelection()
        {
            Selection.objects = SelectedInStage.Except(Selection.gameObjects).ToArray();
        }

        private void IntersectWithSelection()
        {
            Selection.objects = Selection.gameObjects.Intersect(SelectedInStage).ToArray();
        }

        private void SymmetricDifferenceWithSelection()
        {
            GameObject[] selected = Selection.gameObjects;
            Selection.objects = selected
                .Union(SelectedInStage)
                .Except(selected.Intersect(SelectedInStage))
                .ToArray();
        }

        private void OverwriteSelection()
        {
            Selection.objects = SelectedInStage.ToArray();
        }


        private void DeselectWithinStage()
        {
            listView.selectedIndex = -1;
        }

        private void RemoveWithinStage()
        {
            // Not using SelectedInStage since this should not clear the stage when nothing is selected.
            RemoveObjectsFromStage(listViewSelected.Where(obj => obj != null).Cast<GameObject>());
        }

        private void ClearStage()
        {
            staged.Clear();
            stagedLut.Clear();
            listView.Refresh();
        }
    }
}
