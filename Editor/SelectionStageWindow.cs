using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;

// TODO: Figure out how tooltips work.
// TODO: Once upgrading to 2022 add more and better selection support within the stage.
// TODO: dedup - remove objects part of the same prefab

namespace JanSharp
{
    public class SelectionStageWindow : EditorWindow
    {
        // This id is purely needed in order to keep stagedLut in sync with staged while having undo and redo
        // support, without having to regenerate the lut every time it is used, as well as only refreshing the
        // UI when an undo/redo actually modified the stage.
        [SerializeField] private ulong currentUniqueStateId = 0uL;
        [SerializeField] private List<GameObject> staged = new List<GameObject>();
        private ulong nextUniqueStateId = 1uL;
        private ulong lastRefreshedUniqueStateId = 0uL;
        private ulong stagedLutAssociatedUniqueStateId = 0uL;
        private HashSet<GameObject> stagedLut = new HashSet<GameObject>();
        private HashSet<GameObject> StagedLut
        {
            get
            {
                if (stagedLutAssociatedUniqueStateId == currentUniqueStateId)
                    return stagedLut;
                stagedLut = new HashSet<GameObject>(staged);
                return stagedLut;
            }
        }
        private ListView listView;
        private List<object> listViewSelected = new List<object>();
        private IEnumerable<GameObject> SelectedInStage => listViewSelected.Any(obj => obj != null)
            ? listViewSelected.Where(obj => obj != null).Cast<GameObject>()
            : staged;
        private Label countLabel;

        [MenuItem("Tools/JanSharp/Selection Stage Window", priority = 500)]
        public static void ShowSelectionStageWindow()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow window = CreateInstance<SelectionStageWindow>();
            window.titleContent = new GUIContent("Selection Stage");
            window.Show();
        }

        private void CreateGUI()
        {
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
                if (obj == null)
                    return;
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
                    RefreshList();
                }
            };
            listView.onSelectionChanged += selected => {
                listViewSelected = selected;
                countLabel.text = GetCountLabelText();
            };
            listBox.Add(listView);

            root.Add(listBox);

            Box buttonsBox = new Box();
            VisualElement buttonColumns = new VisualElement() { style = { flexDirection = FlexDirection.Row } };

            {
                VisualElement column = new VisualElement() { style = { flexGrow = 1f } };
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
                VisualElement column = new VisualElement() { style = { flexGrow = 1f } };
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
                VisualElement column = new VisualElement() { style = { flexGrow = 1f } };
                column.Add(new Label("Within Stage") { style = { alignSelf = Align.Center } });
                column.Add(new Button(DeselectWithinStage) { text = "Deselect" });
                column.Add(new Button(SortWithinStage) { text = "Sort" });
                column.Add(new Button(RemoveWithinStage) { text = "Remove" });
                column.Add(new Button(ClearStage) { text = "Clear" });
                countLabel = new Label(GetCountLabelText())
                {
                    style = {
                        alignSelf = Align.Center,
                        unityTextAlign = TextAnchor.UpperCenter,
                        paddingTop = 2f,
                    },
                };
                column.Add(countLabel);
                buttonColumns.Add(column);
            }

            buttonsBox.Add(buttonColumns);
            root.Add(buttonsBox);

            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnHierarchyChange()
        {
            Cleanup();
        }

        private void OnUndoRedo()
        {
            if (lastRefreshedUniqueStateId == currentUniqueStateId)
                return;
            RefreshList();
        }

        private void OnDestroy()
        {
            // Who knows if this is actually required, but I think it's clean.
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void RefreshList()
        {
            lastRefreshedUniqueStateId = currentUniqueStateId;
            listView.Refresh();
            countLabel.text = GetCountLabelText();
        }

        private void BeginUndoAbleOperation(string name)
        {
            Undo.RecordObject(this, name);
        }

        private string GetCountLabelText()
        {
            int selectedCount = SelectedInStage.Count();
            return selectedCount == 0 || selectedCount == staged.Count
                ? $"count:\n{staged.Count}"
                : $"count:\n{selectedCount}/{staged.Count}";
        }

        private void EndUndoAbleOperation()
        {
            currentUniqueStateId = nextUniqueStateId++;
            lastRefreshedUniqueStateId = currentUniqueStateId;
        }

        private void MarkStagedLutAsUpToDate()
        {
            stagedLutAssociatedUniqueStateId = currentUniqueStateId;
        }

        private void RemoveObjectsFromStage(IEnumerable<GameObject> toRemove, bool inverted = false)
        {
            if (!inverted && !toRemove.Any())
                return;
            BeginUndoAbleOperation("Removed from Selection Stage");
            int c = staged.Count;
            int newI = 0;
            for (int i = 0; i < c; i++)
            {
                GameObject go = staged[i];
                if (toRemove.Contains(go) != inverted)
                    StagedLut.Remove(go);
                else
                    staged[newI++] = go;
            }
            staged.RemoveRange(newI, c - newI);
            EndUndoAbleOperation();
            MarkStagedLutAsUpToDate();
            listView.selectedIndex = -1;
            RefreshList();
        }

        private void OverwriteStageEntirely(ICollection<GameObject> newSelection)
        {
            BeginUndoAbleOperation("Set Selection State");
            staged.Clear();
            staged.AddRange(newSelection);
            stagedLut = new HashSet<GameObject>(staged);
            EndUndoAbleOperation();
            MarkStagedLutAsUpToDate();
            listView.selectedIndex = -1;
            RefreshList();
        }

        private void Cleanup()
        {
            RemoveObjectsFromStage(staged.Where(go => go == null));
        }


        private void AddToStage()
        {
            BeginUndoAbleOperation("Add to Selection Stage");
            foreach (GameObject go in Selection.gameObjects)
            {
                if (StagedLut.Contains(go))
                    continue;
                StagedLut.Add(go);
                staged.Add(go);
            }
            EndUndoAbleOperation();
            MarkStagedLutAsUpToDate();
            RefreshList();
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

        private class GameObjectComparer : Comparer<GameObject>
        {
            public override int Compare(GameObject x, GameObject y)
            {
                return x.name.ToLower().CompareTo(y.name.ToLower());
            }
        }

        private void SortWithinStage()
        {
            Cleanup();
            BeginUndoAbleOperation("Sort Selection Stage");
            staged.Sort(new GameObjectComparer());
            EndUndoAbleOperation();
            MarkStagedLutAsUpToDate();
            listView.selectedIndex = -1;
            RefreshList();
        }

        private void RemoveWithinStage()
        {
            // Not using SelectedInStage since this should not clear the stage when nothing is selected.
            RemoveObjectsFromStage(listViewSelected.Where(obj => obj != null).Cast<GameObject>());
        }

        private void ClearStage()
        {
            BeginUndoAbleOperation("Clear Selection Stage");
            staged.Clear();
            stagedLut.Clear();
            EndUndoAbleOperation();
            MarkStagedLutAsUpToDate();
            listView.selectedIndex = -1;
            RefreshList();
        }


        public void SetStage(ICollection<GameObject> newStage)
        {
            OverwriteStageEntirely(newStage);
        }
    }
}
