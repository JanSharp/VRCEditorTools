using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;

namespace JanSharp
{
    public class StaticFlagsHelperWindow : EditorWindow
    {
        ///cSpell:ignore Occluder, Occludee

        private PerFlagData[] allFlags = new PerFlagData[]
        {
            new PerFlagData(StaticEditorFlags.ContributeGI, "Contribute GI"),
            new PerFlagData(StaticEditorFlags.OccluderStatic, "Occluder Static"),
            new PerFlagData(StaticEditorFlags.OccludeeStatic, "Occludee Static"),
            new PerFlagData(StaticEditorFlags.BatchingStatic, "Batching Static"),
            new PerFlagData(StaticEditorFlags.NavigationStatic, "Navigation Static"),
            new PerFlagData(StaticEditorFlags.OffMeshLinkGeneration, "Off Mesh Link Generation"),
            new PerFlagData(StaticEditorFlags.ReflectionProbeStatic, "Reflection Probe Static"),
        };

        private class PerFlagData
        {
            public StaticEditorFlags flag;
            public string name;
            public TogglePair visibilityToggles;
            public TogglePair modificationToggles;

            public PerFlagData(StaticEditorFlags flag, string name)
            {
                this.flag = flag;
                this.name = name;
                visibilityToggles = new TogglePair();
                modificationToggles = new TogglePair();
            }
        }

        private class TogglePair
        {
            public Toggle on;
            public Toggle off;
        }

        [MenuItem("Tools/JanSharp/Static Flags Helper Window", priority = 500)]
        public static void ShowStaticFlagsHelperWindow()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<StaticFlagsHelperWindow>();
            wnd.titleContent = new GUIContent("Static Flags Helper");
        }

        private VisualElement CreateFlagPlusToggleList(System.Func<PerFlagData, TogglePair> getPair, string headerPrefix)
        {
            VisualElement columns = new VisualElement() {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignContent = Align.Center,
                    marginBottom = 4f,
                },
            };
            {
                VisualElement column = new VisualElement() { style = { flexGrow = 1f } };
                column.Add(new Label("Flag") { style = { paddingBottom = 4f, alignSelf = Align.Center } });
                foreach (PerFlagData data in allFlags)
                    column.Add(new Label(data.name) { style = { flexGrow = 1f } });
                columns.Add(column);
            }
            {
                VisualElement column = new VisualElement() { style = { flexGrow = 1f } };
                column.Add(new Label(headerPrefix + " Set") { style = { alignSelf = Align.Center, paddingBottom = 4f } });
                foreach (PerFlagData data in allFlags)
                {
                    TogglePair pair = getPair(data);
                    column.Add(pair.on = new Toggle() { style = { alignSelf = Align.Center } });
                    pair.on.RegisterValueChangedCallback(e => { if (e.newValue) pair.off.value = false; });
                }
                columns.Add(column);
            }
            {
                VisualElement column = new VisualElement() { style = { flexGrow = 1f } };
                column.Add(new Label(headerPrefix + " Unset") { style = { alignSelf = Align.Center, paddingBottom = 4f } });
                foreach (PerFlagData data in allFlags)
                {
                    TogglePair pair = getPair(data);
                    column.Add(pair.off = new Toggle() { style = { alignSelf = Align.Center } });
                    pair.off.RegisterValueChangedCallback(e => { if (e.newValue) pair.on.value = false; });
                }
                columns.Add(column);
            }
            return columns;
        }

        private VisualElement CreateVisibilityPanel()
        {
            Box box = new Box();
            Foldout foldout = new Foldout() { text = "Visibility" };

            foldout.Add(CreateFlagPlusToggleList(data => data.visibilityToggles, "Is"));

            VisualElement buttonRow = new VisualElement() {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignContent = Align.Center,
                    marginBottom = 2f,
                },
            };
            buttonRow.Add(new Button(OnShowAllClick) { text = "Show All", style = { flexGrow = 1f } });
            buttonRow.Add(new Button(OnMatchingAnyClick) { text = "Matching Any", style = { flexGrow = 1f } });
            buttonRow.Add(new Button(OnMatchingAllClick) { text = "Matching All", style = { flexGrow = 1f } });
            foldout.Add(buttonRow);

            box.Add(foldout);
            return box;
        }

        private VisualElement CreateModificationPanel()
        {
            Box box = new Box();
            Foldout foldout = new Foldout() { text = "Modification" };

            foldout.Add(CreateFlagPlusToggleList(data => data.modificationToggles, "Do"));

            VisualElement columns = new VisualElement() {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignContent = Align.Center,
                    marginBottom = 2f,
                },
            };
            {
                VisualElement column = new VisualElement() { style = { flexGrow = 1f } };
                column.Add(new Button(Apply) { text = "Apply" });
                // column.Add(new Button(ApplyToPrefab) { text = "Apply To Prefab" });
                column.Add(new Button(RevertFlagsOverrides) { text = "Revert Overrides" });
                columns.Add(column);
            }
            {
                VisualElement column = new VisualElement() { style = { flexGrow = 1f } };
                column.Add(new Button(ApplyRecursive) { text = "Also In Children" });
                // column.Add(new Button(ApplyToPrefabRecursive) { text = "Also In Children" });
                column.Add(new Button(RevertFlagsOverridesRecursive) { text = "Also In Children" });
                columns.Add(column);
            }
            foldout.Add(columns);

            box.Add(foldout);
            return box;
        }

        private void OnShowAllClick()
        {
            SceneVisibilityManager.instance.ShowAll();
        }

        private void OnMatchingAnyClick()
        {
            SceneVisibilityManager.instance.HideAll();
            GetOnOffFlags(data => data.visibilityToggles, out var onFlags, out var offFlags);
            UpdateVisibility(flags => (flags & onFlags) != 0 || ((~flags) & offFlags) != 0);
        }

        private void OnMatchingAllClick()
        {
            SceneVisibilityManager.instance.HideAll();
            GetOnOffFlags(data => data.visibilityToggles, out var onFlags, out var offFlags);
            UpdateVisibility(flags => (flags & onFlags) == onFlags && ((~flags) & offFlags) == offFlags);
        }

        private void GetOnOffFlags(System.Func<PerFlagData, TogglePair> getPair, out StaticEditorFlags onFlags, out StaticEditorFlags offFlags)
        {
            onFlags = 0;
            offFlags = 0;
            foreach (PerFlagData data in allFlags)
            {
                TogglePair pair = getPair(data);
                onFlags |= pair.on.value ? data.flag : 0;
                offFlags |= pair.off.value ? data.flag : 0;
            }
        }

        private void UpdateVisibility(System.Func<StaticEditorFlags, bool> condition)
        {
            SceneVisibilityManager.instance.HideAll();
            void WalkGameObject(GameObject go)
            {
                foreach (Transform transform in go.transform)
                    WalkGameObject(transform.gameObject);
                if (condition(GameObjectUtility.GetStaticEditorFlags(go)))
                    SceneVisibilityManager.instance.Show(go, false);
            }
            foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                WalkGameObject(go);
        }

        private IEnumerable<GameObject> GetSelectedAndChildren()
        {
            // Lazy Linq user alert.
            return Selection.gameObjects.SelectMany(go => go.transform.GetComponentsInChildren<Transform>().Select(t => t.gameObject));
        }

        private void Apply() => ApplyInternal(Selection.gameObjects);

        private void ApplyRecursive() => ApplyInternal(GetSelectedAndChildren());

        private void ApplyInternal(IEnumerable<GameObject> gos)
        {
            GetOnOffFlags(data => data.modificationToggles, out var onFlags, out var offFlags);
            foreach (GameObject go in gos)
            {
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                StaticEditorFlags modifiedFlags = (flags | onFlags) & (~offFlags);
                if (modifiedFlags == flags)
                    continue;
                SerializedObject goProxy = new SerializedObject(go);
                goProxy.FindProperty("m_StaticEditorFlags").intValue = (int)modifiedFlags;
                goProxy.ApplyModifiedProperties();
            }
        }

        // TODO: These should do the same as Apply except that if any modified object is part of a prefab instance
        // it should modify the prefab instead of applying the modifications as overrides.

        // private void ApplyToPrefab()
        // {

        // }

        // private void ApplyToPrefabRecursive()
        // {

        // }

        private void RevertFlagsOverrides() => RevertInternal(Selection.gameObjects);

        private void RevertFlagsOverridesRecursive() => RevertInternal(GetSelectedAndChildren());

        private void RevertInternal(IEnumerable<GameObject> gos)
        {
            foreach (GameObject go in gos)
            {
                SerializedObject goProxy = new SerializedObject(go);
                PrefabUtility.RevertPropertyOverride(goProxy.FindProperty("m_StaticEditorFlags"), InteractionMode.UserAction);
                goProxy.ApplyModifiedProperties();
            }
        }

        public void CreateGUI()
        {
            ScrollView scrollView = new ScrollView();
            VisualElement panel = CreateVisibilityPanel();
            panel.style.marginBottom = 2f;
            scrollView.Add(panel);
            scrollView.Add(CreateModificationPanel());
            rootVisualElement.Add(scrollView);
        }
    }
}
