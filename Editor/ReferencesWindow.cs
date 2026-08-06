using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace JanSharp
{
    public class ReferencesWindow : EditorWindow
    {
        #region Incoming
        /// <summary>
        /// <para>Purely contains references coming from other components in the scene.</para>
        /// </summary>
        Dictionary<Component, List<Component>> refsIncomingToComponents = new Dictionary<Component, List<Component>>();
        /// <summary>
        /// <para>Contains references to game objects in the scene coming from other components in the scene.</para>
        /// <para>Also contains keys which are assets that are references by components in the scene.</para>
        /// </summary>
        Dictionary<Object, List<Component>> refsIncomingToObjects = new Dictionary<Object, List<Component>>();
        private int totalComponentRefsCount = 0;
        private int totalOtherRefsCount = 0;
        #endregion

        #region Outgoing
        /// <summary>
        /// <para>Only contains references from components in the scene to any other components or game
        /// objects also in the scene. No references to assets.</para>
        /// </summary>
        Dictionary<Component, List<Object>> refsOutgoingFromComponents = new Dictionary<Component, List<Object>>();
        #endregion

        private Label totalRefsCountLabel;
        private VisualElement container;
        private Toggle autoUpdateToggle;
        private Toggle includeChildrenToggle;
        private Button updateButton;
        private Button pingSelfButton;

        private bool isShowingReferences = false;
        private Object currentSelf = null;

        [MenuItem("Tools/JanSharp/References Window", priority = 500)]
        public static void ShowReferencesWindow()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<ReferencesWindow>();
            wnd.titleContent = new GUIContent("References");
        }

        private void CreateGUI()
        {
            ClearCollectedReferences();

            VisualElement root = this.rootVisualElement;
            ScrollView scrollView = new ScrollView();

            AddCollectAllReferencesButton(scrollView);
            AddTotalRefsCountLabel(scrollView);
            AddUpdateAndPingButtons(scrollView);
            AddAutoUpdateToggle(scrollView);
            AddIncludeChildrenToggle(scrollView);
            AddContainer(scrollView);

            root.Add(scrollView);

            UpdateUpdateButton();
            UpdatePingSelfButton();
        }

        private void AddCollectAllReferencesButton(VisualElement parent)
        {
            parent.Add(new Button(CollectAllReferences) { text = "Collect all references in active scene" });
        }

        private void AddTotalRefsCountLabel(VisualElement parent)
        {
            totalRefsCountLabel = new Label(GetRefCountLabelText());
            totalRefsCountLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            totalRefsCountLabel.style.marginBottom = 4f;
            parent.Add(totalRefsCountLabel);
        }

        private void AddAutoUpdateToggle(VisualElement parent)
        {
            autoUpdateToggle = new Toggle("Auto Update On Selection Change");
            autoUpdateToggle.value = true;
            autoUpdateToggle.RegisterValueChangedCallback(e =>
            {
                UpdateUpdateButton();
                if (e.newValue)
                    UpdateForSelected();
            });
            parent.Add(autoUpdateToggle);
        }

        private void AddIncludeChildrenToggle(VisualElement parent)
        {
            includeChildrenToggle = new Toggle("Include References To/From Children");
            includeChildrenToggle.RegisterValueChangedCallback(value =>
            {
                if (autoUpdateToggle.value)
                    UpdateForSelected();
            });
            parent.Add(includeChildrenToggle);
        }

        private void AddUpdateAndPingButtons(VisualElement parent)
        {
            VisualElement horizontalButtons = new VisualElement() { style = { flexDirection = FlexDirection.Row } };

            updateButton = new Button(UpdateForSelected) { text = "Update For Selected", style = { flexGrow = 1f } };
            horizontalButtons.Add(updateButton);

            pingSelfButton = new Button(PingSelf) { text = "Ping Self", style = { flexGrow = 1f } };
            horizontalButtons.Add(pingSelfButton);

            parent.Add(horizontalButtons);
        }

        private void AddContainer(VisualElement parent)
        {
            container = new VisualElement();
            container.style.marginTop = 4f;
            parent.Add(container);
        }

        private void OnSelectionChange()
        {
            if (autoUpdateToggle.value)
                UpdateForSelected();
        }

        private void UpdateUpdateButton()
        {
            updateButton.SetEnabled(!autoUpdateToggle.value);
        }

        private void UpdatePingSelfButton()
        {
            pingSelfButton.SetEnabled(isShowingReferences);
        }

        private void SetIsShowingReferences(bool isShowing)
        {
            isShowingReferences = isShowing;
            if (!isShowing)
                currentSelf = null;
            UpdatePingSelfButton();
        }

        private void PingSelf()
        {
            if (currentSelf != null) // Could have been destroyed.
                EditorGUIUtility.PingObject(currentSelf);
        }

        private void ClearContainer()
        {
            container.Clear();
            SetIsShowingReferences(false);
        }

        private void UpdateForSelected()
        {
            ClearContainer();
            Object selected = Selection.activeObject;
            if (selected == null)
                return;
            currentSelf = selected;
            SetIsShowingReferences(true);
            if (includeChildrenToggle.value && currentSelf is GameObject go && !PrefabUtility.IsPartOfPrefabAsset(go))
                UpdateContainerIncludingChildren(go);
            else
                UpdateContainerForSingleObject(currentSelf);
        }

        private void AddFoldout<T>(bool isIncoming, List<T> referees, string referencedObjectName = null) where T : Object
        {
            Box box = new Box();
            box.style.marginTop = 2f;

            Foldout foldout = new Foldout();
            if (isIncoming)
                foldout.text = $"Incoming references{(referencedObjectName == null ? "" : $" to {referencedObjectName}")}: {referees.Count}";
            else
                foldout.text = $"Outgoing references{(referencedObjectName == null ? "" : $" from {referencedObjectName}")}: {referees.Count}";

            foreach (Object referee in referees)
                foldout.contentContainer.Add(new Button(() => { EditorGUIUtility.PingObject(referee); })
                {
                    text = $"{referee.name} - {referee.GetType().Name}",
                });

            box.Add(foldout);
            container.Add(box);
        }

        private void AddNoReferencesBox(string label)
        {
            Box box = new Box();
            box.style.marginTop = 2f;
            box.style.paddingBottom = 2f;
            box.style.paddingTop = 2f;
            box.style.paddingLeft = 4f;
            box.style.paddingRight = 4f;
            box.Add(new Label(label) { style = { unityTextAlign = TextAnchor.MiddleCenter } });
            container.Add(box);
        }

        private void AddHeader(string label, bool extraTopMargin = false)
        {
            container.Add(new Label(label)
            {
                style =
                {
                    fontSize = 14f,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginTop = extraTopMargin ? 8f : 2f,
                },
            });
        }

        private void UpdateContainerIncludingChildren(GameObject parent)
        {
            List<Component> components = parent.GetComponentsInChildren<Component>(includeInactive: true).Where(c => c != null).ToList();
            List<GameObject> gameObjects = components.Select(c => (c as Transform)?.gameObject).Where(go => go != null).ToList();
            HashSet<Object> innerObjectsLut = new HashSet<Object>(components);
            foreach (GameObject go in gameObjects)
                innerObjectsLut.Add(go);

            HashSet<Object> incomingRefs = new HashSet<Object>();
            HashSet<Object> outgoingRefs = new HashSet<Object>();

            foreach (Component component in components)
            {
                if (refsIncomingToComponents.TryGetValue(component, out List<Component> refs))
                    foreach (Component comp in refs)
                        if (!innerObjectsLut.Contains(comp) && !incomingRefs.Contains(comp))
                            incomingRefs.Add(comp);

                if (refsOutgoingFromComponents.TryGetValue(component, out List<Object> objs))
                    foreach (Object obj in objs)
                        if (!innerObjectsLut.Contains(obj) && !outgoingRefs.Contains(obj))
                            outgoingRefs.Add(obj);
            }

            AddHeader("Incoming References");
            if (incomingRefs.Count == 0)
                AddNoReferencesBox("No incoming references to selected and children");
            else
                AddFoldout(isIncoming: true, incomingRefs.ToList());

            AddHeader("Outgoing References", extraTopMargin: true);
            if (outgoingRefs.Count == 0)
                AddNoReferencesBox("No outgoing references from selected and children");
            else
                AddFoldout(isIncoming: false, outgoingRefs.ToList());
        }

        private void UpdateContainerForSingleObject(Object main)
        {
            bool noIncomingReferences = true;
            bool noOutgoingReferences = true;

            bool isGameObject = main is GameObject;
            Component[] components = !isGameObject ? null : ((GameObject)main).GetComponents<Component>();

            AddHeader("Incoming References");
            if (refsIncomingToObjects.TryGetValue(main, out List<Component> incomingRefs))
            {
                noIncomingReferences = false;
                AddFoldout(isIncoming: true, incomingRefs, referencedObjectName: isGameObject ? "GameObject" : "Asset");
            }
            if (isGameObject)
                foreach (Component component in components)
                    if (component != null && refsIncomingToComponents.TryGetValue(component, out incomingRefs))
                    {
                        noIncomingReferences = false;
                        AddFoldout(isIncoming: true, incomingRefs, component.GetType().Name);
                    }
            if (noIncomingReferences)
                AddNoReferencesBox("No incoming references to selected");

            AddHeader("Outgoing References", extraTopMargin: true);
            if (isGameObject)
                foreach (Component component in components)
                    if (component != null && refsOutgoingFromComponents.TryGetValue(component, out List<Object> outgoingRefs))
                    {
                        noOutgoingReferences = false;
                        AddFoldout(isIncoming: false, outgoingRefs, component.GetType().Name);
                    }
            if (noOutgoingReferences)
                AddNoReferencesBox("No outgoing references from selected");
        }

        private string GetRefCountLabelText()
        {
            return $"{totalComponentRefsCount} component refs, {totalOtherRefsCount} other refs";
        }

        private void AddIncomingRef<T>(Dictionary<T, List<Component>> refs, T referenced, Component referee, ref int count)
        {
            if (!refs.TryGetValue(referenced, out List<Component> referees))
            {
                referees = new List<Component>();
                refs.Add(referenced, referees);
            }
            else if (referees.Contains(referee))
                return;
            count++;
            referees.Add(referee);
        }

        private void ClearCollectedReferences()
        {
            refsIncomingToComponents.Clear();
            refsIncomingToObjects.Clear();
            totalComponentRefsCount = 0;
            totalOtherRefsCount = 0;
            refsOutgoingFromComponents.Clear();
            if (totalRefsCountLabel != null)
                totalRefsCountLabel.text = GetRefCountLabelText();
        }

        private void CollectAllReferences()
        {
            ClearCollectedReferences();
            List<Object> outgoingRefs = new List<Object>();
            foreach (Component referee in UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Component>(includeInactive: true))
                .Where(c => c != null))
            {
                if (referee is Transform)
                    continue;
                GameObject refereeGameObject = referee.gameObject;
                SerializedObject proxy = new SerializedObject(referee);
                SerializedProperty iter = proxy.GetIterator();
                if (!iter.Next(enterChildren: true))
                    continue;
                do
                {
                    if (iter.propertyType != SerializedPropertyType.ObjectReference)
                        continue;
                    Object referencedObject = iter.objectReferenceValue;
                    if (referencedObject == null || referencedObject == referee || referencedObject == refereeGameObject)
                        continue;
                    if (referencedObject is Component referencedComponent)
                    {
                        AddIncomingRef(refsIncomingToComponents, referencedComponent, referee, ref totalComponentRefsCount);
                        // Prefab asset references are currently not needed, so just filter them out right away.
                        if (!PrefabUtility.IsPartOfPrefabAsset(referencedObject))
                            outgoingRefs.Add(referencedObject);
                    }
                    else
                    {
                        AddIncomingRef(refsIncomingToObjects, referencedObject, referee, ref totalOtherRefsCount);
                        // Prefab asset references are currently not needed, so just filter them out right away.
                        // Same for non game object asset references.
                        if (referencedObject is GameObject && !PrefabUtility.IsPartOfPrefabAsset(referencedObject))
                            outgoingRefs.Add(referencedObject);
                    }
                }
                while (iter.Next(true));

                if (outgoingRefs.Count != 0)
                {
                    refsOutgoingFromComponents.Add(referee, new List<Object>(outgoingRefs));
                    outgoingRefs.Clear();
                }
            }

            totalRefsCountLabel.text = GetRefCountLabelText();
            if (autoUpdateToggle.value)
                UpdateForSelected();
        }
    }
}
