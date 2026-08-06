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

        private Label totalRefCountLabel;
        private VisualElement container;
        private Toggle autoUpdateToggle;
        private Toggle includeChildrenToggle;
        private Button updateButton;

        [MenuItem("Tools/JanSharp/References Window", priority = 500)]
        public static void ShowReferencesWindow()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<ReferencesWindow>();
            wnd.titleContent = new GUIContent("References");
        }

        private void CreateGUI()
        {
            ClearDataset();

            VisualElement root = this.rootVisualElement;

            ScrollView scrollView = new ScrollView();

            scrollView.Add(new Button(RefreshDataset) { text = "Refresh Dataset" });
            totalRefCountLabel = new Label(GetRefCountLabelText());
            totalRefCountLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            scrollView.Add(totalRefCountLabel);

            autoUpdateToggle = new Toggle("Auto Update");
            autoUpdateToggle.value = true;
            autoUpdateToggle.RegisterValueChangedCallback(e =>
            {
                UpdateUpdateButton();
                if (e.newValue)
                    UpdateForSelected();
            });
            scrollView.Add(autoUpdateToggle);

            includeChildrenToggle = new Toggle("Include Children");
            includeChildrenToggle.RegisterValueChangedCallback(value =>
            {
                if (autoUpdateToggle.value)
                    UpdateForSelected();
            });
            scrollView.Add(includeChildrenToggle);

            updateButton = new Button(UpdateForSelected) { text = "Update" };
            UpdateUpdateButton();
            scrollView.Add(updateButton);

            container = new VisualElement();
            container.style.marginTop = 4;
            scrollView.Add(container);

            root.Add(scrollView);
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

        private void UpdateForSelected()
        {
            container.Clear();
            Object selected = Selection.activeObject;
            if (selected == null)
                return;
            if (includeChildrenToggle.value && selected is GameObject go && !PrefabUtility.IsPartOfPrefabAsset(go))
                UpdateContainerIncludingChildren(go);
            else
                UpdateContainerForSingleObject(selected);
        }

        private void AddFoldout<T>(bool isIncoming, List<T> referees, string referencedObjectName = null) where T : Object
        {
            Box box = new Box();
            box.style.marginTop = 2;

            Foldout foldout = new Foldout();
            if (isIncoming)
                foldout.text = $"Incoming refs{(referencedObjectName == null ? "" : $" from {referencedObjectName}")}: {referees.Count}";
            else
                foldout.text = $"Outgoing refs{(referencedObjectName == null ? "" : $" to {referencedObjectName}")}: {referees.Count}";

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
            box.style.marginTop = 2;
            box.Add(new Label(label));
            container.Add(box);
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

            if (incomingRefs.Count == 0)
                AddNoReferencesBox("No incoming references to selected and children");
            else
                AddFoldout(isIncoming: true, incomingRefs.ToList());

            if (outgoingRefs.Count == 0)
                AddNoReferencesBox("No outgoing references from selected and children");
            else
                AddFoldout(isIncoming: false, outgoingRefs.ToList());
        }

        private void UpdateContainerForSingleObject(Object main)
        {
            bool noReferences = true;

            bool isGameObject = main is GameObject;

            if (refsIncomingToObjects.TryGetValue(main, out List<Component> refs))
            {
                noReferences = false;
                AddFoldout(isIncoming: true, refs, referencedObjectName: isGameObject ? "GameObject" : "Asset");
            }

            if (isGameObject)
                foreach (Component component in ((GameObject)main).GetComponents<Component>())
                    if (component != null && refsIncomingToComponents.TryGetValue(component, out refs))
                    {
                        noReferences = false;
                        AddFoldout(isIncoming: true, refs, component.GetType().Name);
                    }

            if (noReferences)
                AddNoReferencesBox("No incoming references to selected.");
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

        private void ClearDataset()
        {
            refsIncomingToComponents.Clear();
            refsIncomingToObjects.Clear();
            totalComponentRefsCount = 0;
            totalOtherRefsCount = 0;
            refsOutgoingFromComponents.Clear();
            if (totalRefCountLabel != null)
                totalRefCountLabel.text = GetRefCountLabelText();
        }

        private void RefreshDataset()
        {
            ClearDataset();
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

            totalRefCountLabel.text = GetRefCountLabelText();
            if (autoUpdateToggle.value)
                UpdateForSelected();
        }
    }
}
