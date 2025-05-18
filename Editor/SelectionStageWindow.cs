using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace JanSharp
{
    public class SelectionStageWindow : EditorWindow
    {
        // This id is purely needed in order to keep stagedLut in sync with staged while having undo and redo
        // support, without having to regenerate the lut every time it is used, as well as only refreshing the
        // UI when an undo/redo actually modified the stage.
        [SerializeField] private ulong currentUniqueStateId = 0uL;
        [SerializeField] private List<Object> staged = new List<Object>();
        private ulong nextUniqueStateId = 1uL;
        private ulong lastRefreshedUniqueStateId = 0uL;
        private ulong stagedLutAssociatedUniqueStateId = 0uL;
        private HashSet<Object> stagedLut = new HashSet<Object>();
        private HashSet<Object> StagedLut
        {
            get
            {
                if (stagedLutAssociatedUniqueStateId == currentUniqueStateId)
                    return stagedLut;
                stagedLut = new HashSet<Object>(staged);
                return stagedLut;
            }
        }
        private ListView listView;
        private List<object> listViewSelected = new List<object>();
        private IEnumerable<Object> SelectedInStage => listViewSelected.Any(obj => obj != null)
            ? listViewSelected.Where(obj => obj != null).Cast<Object>()
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

        private static bool IsAsset(Object obj)
        {
            return obj is not GameObject || AssetDatabase.IsMainAsset(obj);
        }

        private static string GetHierarchyPath(Transform t, string prefix, string separator)
        {
            StringBuilder sb = new StringBuilder(prefix);
            void Add(Transform parent)
            {
                if (parent == null)
                    return;
                Add(parent.parent);
                sb.Append(parent.name);
                if (parent != t)
                    sb.Append(separator);
            }
            Add(t);
            return sb.ToString();
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            Box listBox = new Box() { style = { flexGrow = 1f } };

            System.Func<VisualElement> makeItem = () =>
            {
                VisualElement row = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                row.Add(new Image() { style = { width = 18f, flexShrink = 0f } });
                row.Add(new Label());
                return row;
            };
            System.Action<VisualElement, int> bindItem = (element, index) =>
            {
                Object obj = staged[index];
                string tooltip = IsAsset(obj)
                    ? "Asset:\n  " + AssetDatabase.GetAssetPath(obj).Replace("/", "\n  ")
                    : GetHierarchyPath(((GameObject)obj).transform, "Hierarchy:\n  ", "\n  ");
                Image image = (Image)element[0];
                Label label = (Label)element[1];
                image.image = AssetPreview.GetMiniThumbnail(obj);
                image.tooltip = tooltip; // Having the tooltip on the entire row is frankly annoying.
                label.text = obj.name;
            };
            listView = new ListView(staged, 16, makeItem, bindItem)
            {
                style = { flexGrow = 1f },
                selectionType = SelectionType.Multiple,
            };
            listView.itemsChosen += obj =>
            {
                Object go = (Object)obj.FirstOrDefault();
                if (go == null)
                    return;
                EditorGUIUtility.PingObject(go); // Jump to in hierarchy, without selecting.
                var prev = Selection.objects;
                Selection.activeObject = go;
                SceneView.FrameLastActiveSceneView(); // Does nothing if the selected object is not in the hierarchy.
                Selection.objects = prev;
                if (listViewSelected.Count == 1 && ((Object)listViewSelected[0]) == go)
                {
                    // When double clicking a single element, clear the selection again as to prevent
                    // accidentally narrowing the stage through having only 1 element selected.
                    listView.ClearSelection();
                    RefreshList();
                }
            };
            listView.selectionChanged += selected =>
            {
                listViewSelected = selected.ToList();
                countLabel.text = GetCountLabelText();
            };
            listBox.Add(listView);

            root.Add(listBox);

            Box buttonsBox = new Box() { style = { flexShrink = 0f } };
            VisualElement buttonColumns = new VisualElement() { style = { flexDirection = FlexDirection.Row } };

            {
                VisualElement column = new VisualElement() { style = { flexGrow = 1f } };
                column.Add(new Label("Affect Stage")
                {
                    style = { alignSelf = Align.Center },
                    tooltip = "How should the current selection affect the stage?",
                });
                column.Add(new Button(OverwriteStage)
                {
                    text = "Overwrite",
                    tooltip = "Set stage equal to the current selection",
                });
                column.Add(new Button(AddToStage)
                {
                    text = "Add",
                    tooltip = "Add selected objects to the stage",
                });
                column.Add(new Button(ExcludeFromStage)
                {
                    text = "Exclude",
                    tooltip = "Remove selected objects from the stage",
                });
                column.Add(new Button(FlipExcludeFromStage)
                {
                    text = "Flip Exclude",
                    tooltip = "Add selected objects to the stage, "
                        + "then remove objects that were already in the stage previously",
                });
                column.Add(new Button(IntersectWithStage)
                {
                    text = "Intersect",
                    tooltip = "Remove not selected objects from the stage",
                });
                column.Add(new Button(SymmetricDifferenceWithStage)
                {
                    text = "Symmetric Diff",
                    tooltip = "The inverse of Intersect. Add selected objects to the stage, "
                        + "then remove objects that were both staged and selected previously",
                });
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
                column.Add(new Button(OverwriteSelection)
                {
                    text = "Overwrite",
                    tooltip = "Set selection equal to the stage",
                });
                column.Add(new Button(AddToSelection)
                {
                    text = "Add",
                    tooltip = "Add staged objects to selection",
                });
                column.Add(new Button(ExcludeFromSelection)
                {
                    text = "Exclude",
                    tooltip = "Remove staged objects from selection",
                });
                column.Add(new Button(FlipExcludeFromSelection)
                {
                    text = "Flip Exclude",
                    tooltip = "Add staged objects to the selection, "
                        + "then deselect objects that were already selected previously",
                });
                column.Add(new Button(IntersectWithSelection)
                {
                    text = "Intersect",
                    tooltip = "Remove non staged objects from selection",
                });
                column.Add(new Button(SymmetricDifferenceWithSelection)
                {
                    text = "Symmetric Diff",
                    tooltip = "The inverse of Intersect. Add staged objects to selection, "
                        + "then deselect objects that were both staged and selected previously",
                });
                buttonColumns.Add(column);
            }

            {
                VisualElement column = new VisualElement() { style = { flexGrow = 1f } };
                column.Add(new Label("Within Stage") { style = { alignSelf = Align.Center }, });
                column.Add(new Button(DeselectWithinStage) { text = "Deselect" });
                column.Add(new Button(SortWithinStageAlphabetically) { text = "Sort A", tooltip = "Sorts alphabetically, case insensitive" });
                column.Add(new Button(SortWithinStageHierarchy) { text = "Sort H", tooltip = "Sorts based on hierarchy or asset path" });
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
            if (!Cleanup())
                RefreshListWithoutUndo();
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
            RefreshListWithoutUndo();
        }

        private void RefreshListWithoutUndo()
        {
            listView.Rebuild();
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
                ? $"{staged.Count}"
                : $"{selectedCount}/{staged.Count}";
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

        private HashSet<Object> RememberStageSelection()
        {
            HashSet<Object> selectedObjects = listView.selectedIndices.Select(i => staged[i]).ToHashSet();
            listView.ClearSelection();
            return selectedObjects;
        }

        private void RestoreStageSelection(HashSet<Object> selectedObjects)
        {
            for (int i = 0; i < staged.Count; i++)
            {
                Object obj = staged[i];
                if (selectedObjects.Contains(obj))
                    listView.AddToSelection(i);
            }
        }

        /// <returns><see langword="false"/> if nothing happened.</returns>
        private bool RemoveObjectsFromStage(IEnumerable<Object> toRemove, bool inverted = false)
        {
            if (!inverted && !toRemove.Any())
                return false;
            BeginUndoAbleOperation("Removed from Selection Stage");
            HashSet<Object> selectedObjects = RememberStageSelection();
            int c = staged.Count;
            int newI = 0;
            for (int i = 0; i < c; i++)
            {
                Object go = staged[i];
                if (toRemove.Contains(go) != inverted)
                    StagedLut.Remove(go);
                else
                    staged[newI++] = go;
            }
            staged.RemoveRange(newI, c - newI);
            EndUndoAbleOperation();
            MarkStagedLutAsUpToDate();
            RestoreStageSelection(selectedObjects);
            RefreshList();
            return true;
        }

        private void OverwriteStageEntirely(ICollection<Object> newSelection)
        {
            BeginUndoAbleOperation("Set Selection Stage");
            staged.Clear();
            staged.AddRange(newSelection);
            stagedLut = new HashSet<Object>(staged);
            EndUndoAbleOperation();
            MarkStagedLutAsUpToDate();
            listView.ClearSelection();
            RefreshList();
        }

        /// <returns><see langword="false"/> if nothing happened.</returns>
        private bool Cleanup()
        {
            return RemoveObjectsFromStage(staged.Where(go => go == null));
        }


        private void OverwriteStage()
        {
            OverwriteStageEntirely(Selection.objects);
        }

        private void AddToStage()
        {
            BeginUndoAbleOperation("Add to Selection Stage");
            foreach (Object go in Selection.objects)
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
            RemoveObjectsFromStage(Selection.objects);
        }

        private void FlipExcludeFromStage()
        {
            OverwriteStageEntirely(Selection.objects.Except(SelectedInStage).ToList());
        }

        private void IntersectWithStage()
        {
            RemoveObjectsFromStage(Selection.objects, inverted: true);
        }

        private void SymmetricDifferenceWithStage()
        {
            Object[] selected = Selection.objects;
            OverwriteStageEntirely(SelectedInStage
                .Union(selected)
                .Except(SelectedInStage.Intersect(selected))
                .ToList());
        }


        private void OverwriteSelection()
        {
            Selection.objects = SelectedInStage.ToArray();
        }

        private void AddToSelection()
        {
            Selection.objects = Selection.objects.Union(SelectedInStage).ToArray();
        }

        private void ExcludeFromSelection()
        {
            Selection.objects = Selection.objects.Except(SelectedInStage).ToArray();
        }

        private void FlipExcludeFromSelection()
        {
            Selection.objects = SelectedInStage.Except(Selection.objects).ToArray();
        }

        private void IntersectWithSelection()
        {
            Selection.objects = Selection.objects.Intersect(SelectedInStage).ToArray();
        }

        private void SymmetricDifferenceWithSelection()
        {
            Object[] selected = Selection.objects;
            Selection.objects = selected
                .Union(SelectedInStage)
                .Except(selected.Intersect(SelectedInStage))
                .ToArray();
        }


        private void DeselectWithinStage()
        {
            listView.ClearSelection();
        }

        private class ObjectComparer : Comparer<Object>
        {
            public override int Compare(Object x, Object y)
            {
                return x.name.ToLower().CompareTo(y.name.ToLower());
            }
        }

        private void SortWithinStageAlphabetically()
        {
            Cleanup();
            BeginUndoAbleOperation("Sort Selection Stage");
            HashSet<Object> selectedObjects = RememberStageSelection();
            staged.Sort(new ObjectComparer());
            EndUndoAbleOperation();
            MarkStagedLutAsUpToDate();
            RestoreStageSelection(selectedObjects);
            RefreshList();
        }

        private struct HierarchyAwareObject : System.IComparable<HierarchyAwareObject>
        {
            public Object obj;
            public bool isAsset;
            public string assetPath;
            public List<int> hierarchyIndexes;

            public HierarchyAwareObject(Object obj, bool isAsset, string assetPath, List<int> hierarchyIndexes)
            {
                this.obj = obj;
                this.isAsset = isAsset;
                this.assetPath = assetPath;
                this.hierarchyIndexes = hierarchyIndexes;
            }

            public int CompareTo(HierarchyAwareObject other)
            {
                if (isAsset != other.isAsset)
                    return isAsset ? 1 : -1;
                if (isAsset)
                    return assetPath.CompareTo(other.assetPath);
                for (int i = 0; i < System.Math.Min(hierarchyIndexes.Count, other.hierarchyIndexes.Count); i++)
                {
                    int result = hierarchyIndexes[i].CompareTo(other.hierarchyIndexes[i]);
                    if (result != 0)
                        return result;
                }
                return hierarchyIndexes.Count.CompareTo(other.hierarchyIndexes.Count);
            }
        }

        private void SortWithinStageHierarchy()
        {
            Cleanup();
            BeginUndoAbleOperation("Sort Selection Stage");
            HashSet<Object> selectedObjects = RememberStageSelection();
            List<Object> newStaged = staged
                .Select(obj =>
                {
                    bool isAsset = IsAsset(obj);
                    string assetPath = isAsset ? AssetDatabase.GetAssetPath(obj).ToLower() : null;
                    List<int> hierarchyIndexes = null;
                    if (!isAsset)
                    {
                        hierarchyIndexes = new List<int>();
                        void Add(Transform t)
                        {
                            if (t == null)
                                return;
                            Add(t.parent);
                            hierarchyIndexes.Add(t.GetSiblingIndex());
                        }
                        Add(((GameObject)obj).transform);
                    }
                    return new HierarchyAwareObject(obj, isAsset, assetPath, hierarchyIndexes);
                })
                .OrderBy(o => o)
                .Select(o => o.obj)
                .ToList();
            staged.Clear();
            staged.AddRange(newStaged);
            EndUndoAbleOperation();
            MarkStagedLutAsUpToDate();
            RestoreStageSelection(selectedObjects);
            RefreshList();
        }

        private void RemoveWithinStage()
        {
            // Not using SelectedInStage since this should not clear the stage when nothing is selected.
            RemoveObjectsFromStage(listViewSelected.Where(obj => obj != null).Cast<Object>());
        }

        private void ClearStage()
        {
            BeginUndoAbleOperation("Clear Selection Stage");
            staged.Clear();
            stagedLut.Clear();
            EndUndoAbleOperation();
            MarkStagedLutAsUpToDate();
            listView.ClearSelection();
            RefreshList();
        }


        public void SetStage(ICollection<Object> newStage)
        {
            OverwriteStageEntirely(newStage);
        }
    }
}
