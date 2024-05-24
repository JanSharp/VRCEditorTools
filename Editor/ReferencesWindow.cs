using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace JanSharp
{
    public class ReferencesWindow : EditorWindow
    {
        // incoming
        Dictionary<Component, List<Component>> componentRefs = new Dictionary<Component, List<Component>>();
        Dictionary<Object, List<Component>> otherRefs = new Dictionary<Object, List<Component>>();
        private int componentRefsCount = 0;
        private int otherRefsCount = 0;
        // outgoing
        Dictionary<Component, List<Object>> outgoingObjectRefs = new Dictionary<Component, List<Object>>();

        private Label refCountLabel;
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
            refCountLabel = new Label(GetRefCountLabelText());
            refCountLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            scrollView.Add(refCountLabel);

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

        private void AddFoldout<T>(string name, List<T> referees) where T : Object
        {
            Box box = new Box();
            box.style.marginTop = 2;
            Foldout foldout = new Foldout();
            foldout.text = $"{name} refs: {referees.Count}";
            foreach (Object referee in referees)
                foldout.contentContainer.Add(new Button(() => { EditorGUIUtility.PingObject(referee); })
                    { text = $"{referee.name} - {referee.GetType().Name}" });
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
                if (componentRefs.TryGetValue(component, out List<Component> refs))
                    foreach (Component comp in refs)
                        if (!innerObjectsLut.Contains(comp) && !incomingRefs.Contains(comp))
                            incomingRefs.Add(comp);

                if (outgoingObjectRefs.TryGetValue(component, out List<Object> objs))
                    foreach (Object obj in objs)
                        if (!innerObjectsLut.Contains(obj) && !outgoingRefs.Contains(obj))
                            outgoingRefs.Add(obj);
            }

            if (incomingRefs.Count == 0)
                AddNoReferencesBox("No incoming references to selected and children");
            else
                AddFoldout("Incoming", incomingRefs.ToList());

            if (outgoingRefs.Count == 0)
                AddNoReferencesBox("No outgoing references from selected and children");
            else
                AddFoldout("Outgoing", outgoingRefs.ToList());
        }

        private void UpdateContainerForSingleObject(Object main)
        {
            bool noReferences = true;

            bool isGameObject = main is GameObject;

            if (otherRefs.TryGetValue(main, out List<Component> refs))
            {
                noReferences = false;
                AddFoldout(isGameObject ? "GameObject" : "Asset", refs);
            }

            if (isGameObject)
                foreach (Component component in ((GameObject)main).GetComponents<Component>())
                    if (component != null && componentRefs.TryGetValue(component, out refs))
                    {
                        noReferences = false;
                        AddFoldout(component.GetType().Name, refs);
                    }

            if (noReferences)
                AddNoReferencesBox("No references to selected.");
        }

        private string GetRefCountLabelText()
        {
            return $"{componentRefsCount} component refs, {otherRefsCount} other refs";
        }

        private void AddRef<T>(Dictionary<T, List<Component>> refs, T referenced, Component referee, ref int count)
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
            componentRefs.Clear();
            otherRefs.Clear();
            componentRefsCount = 0;
            otherRefsCount = 0;
            outgoingObjectRefs.Clear();
            if (refCountLabel != null)
                refCountLabel.text = GetRefCountLabelText();
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
                if (!iter.Next(true))
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
                        AddRef(componentRefs, referencedComponent, referee, ref componentRefsCount);
                        // Prefab asset references are currently not needed, so just filter them out right away.
                        if (!PrefabUtility.IsPartOfPrefabAsset(referencedObject))
                            outgoingRefs.Add(referencedObject);
                    }
                    else
                    {
                        AddRef(otherRefs, referencedObject, referee, ref otherRefsCount);
                        // Prefab asset references are currently not needed, so just filter them out right away.
                        // Same for non game object asset references.
                        if (referencedObject is GameObject && !PrefabUtility.IsPartOfPrefabAsset(referencedObject))
                            outgoingRefs.Add(referencedObject);
                    }
                }
                while (iter.Next(true));

                if (outgoingRefs.Count != 0)
                {
                    outgoingObjectRefs.Add(referee, new List<Object>(outgoingRefs));
                    outgoingRefs.Clear();
                }
            }

            refCountLabel.text = GetRefCountLabelText();
            if (autoUpdateToggle.value)
                UpdateForSelected();
        }
    }
}
