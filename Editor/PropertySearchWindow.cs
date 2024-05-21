using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;

namespace JanSharp
{
    public class PropertySearchWindow : EditorWindow
    {
        private static string[] propertyTypeStrings = new string[]
        {
            // "Generic",
            "Integer",
            "Boolean",
            "Float",
            "String",
            "Color",
            "ObjectReference",
            "LayerMask",
            // "Enum",
            "Vector2",
            "Vector3",
            "Vector4",
            "Rect",
            // "ArraySize",
            "Character",
            // "AnimationCurve",
            "Bounds",
            // "Gradient", // Don't know how to get its value.
            "Quaternion",
            // "ExposedReference",
            // "FixedBufferSize",
            "Vector2Int",
            "Vector3Int",
            "RectInt",
            "BoundsInt",
            // "ManagedReference",
        };
        private static SerializedPropertyType[] propertyTypes = new SerializedPropertyType[]
        {
            // SerializedPropertyType.Generic,
            SerializedPropertyType.Integer,
            SerializedPropertyType.Boolean,
            SerializedPropertyType.Float,
            SerializedPropertyType.String,
            SerializedPropertyType.Color,
            SerializedPropertyType.ObjectReference,
            SerializedPropertyType.LayerMask,
            // SerializedPropertyType.Enum,
            SerializedPropertyType.Vector2,
            SerializedPropertyType.Vector3,
            SerializedPropertyType.Vector4,
            SerializedPropertyType.Rect,
            // SerializedPropertyType.ArraySize,
            SerializedPropertyType.Character,
            // SerializedPropertyType.AnimationCurve,
            SerializedPropertyType.Bounds,
            // SerializedPropertyType.Gradient,
            SerializedPropertyType.Quaternion,
            // SerializedPropertyType.ExposedReference,
            // SerializedPropertyType.FixedBufferSize,
            SerializedPropertyType.Vector2Int,
            SerializedPropertyType.Vector3Int,
            SerializedPropertyType.RectInt,
            SerializedPropertyType.BoundsInt,
            // SerializedPropertyType.ManagedReference,
        };
        private static Dictionary<SerializedPropertyType, int> propertyTypeIndexMap = null;
        private int selectedPropertyTypeIndex = 0;

        private Vector2 scrollPosition;

        private bool showSelectedInfoMsg = false;
        private GameObject choosingFromObject;
        private Component choosingFromComponent;
        private string searchTerm = "";

        [SerializeField] private string componentName = "";
        [SerializeField] private string propertyName = "";

        [SerializeField] private long longValue;
        [SerializeField] private bool boolValue;
        [SerializeField] private double doubleValue;
        [SerializeField] private string stringValue;
        [SerializeField] private Color colorValue;
        [SerializeField] private Object objectReferenceValue;
        [SerializeField] private int layerMaskValue;
        [SerializeField] private long enumValue;
        [SerializeField] private Vector2 vector2Value;
        [SerializeField] private Vector3 vector3Value;
        [SerializeField] private Vector4 vector4Value;
        [SerializeField] private Rect rectValue;
        [SerializeField] private string characterValue; // Is it actually a string? Who knows but it makes the most sense.
        [SerializeField] private Bounds boundsValue;
        [SerializeField] private Gradient gradientValue;
        [SerializeField] private Quaternion quaternionValue;
        [SerializeField] private Vector2Int vector2IntValue;
        [SerializeField] private Vector3Int vector3IntValue;
        [SerializeField] private RectInt rectIntValue;
        [SerializeField] private BoundsInt boundsIntValue;

        private int foundCount = -1;
        private SelectionStageWindow selectionStage;

        [MenuItem("Tools/JanSharp/Property Search Window", priority = 500)]
        public static void ShowPropertySearchWindow()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<PropertySearchWindow>();
            wnd.titleContent = new GUIContent("Property Search");
        }

        private void OnGUI()
        {
            propertyTypeIndexMap = propertyTypeIndexMap ?? propertyTypes
                .Select((t, i) => (t, i))
                .ToDictionary(e => e.t, e => e.i);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (GUILayout.Button(new GUIContent("Choose From Selected")))
            {
                if (Selection.activeGameObject == null)
                    showSelectedInfoMsg = true;
                else
                {
                    showSelectedInfoMsg = false;
                    componentName = "";
                    propertyName = "";
                    searchTerm = "";
                    choosingFromObject = Selection.activeGameObject;
                }
            }
            if (showSelectedInfoMsg)
                GUILayout.Label(new GUIContent("Must have a game object selected."));

            if (componentName == "" && choosingFromObject != null)
            {
                EditorGUILayout.Separator();
                foreach (Component component in choosingFromObject.GetComponents<Component>().Where(c => c != null))
                {
                    string name = component.GetType().Name;
                    if (GUILayout.Button(new GUIContent(name)))
                    {
                        componentName = name;
                        choosingFromComponent = component;
                    }
                }
                EditorGUILayout.Separator();
            }
            else if (propertyName == "" && choosingFromComponent != null)
            {
                EditorGUILayout.Separator();
                searchTerm = GUILayout.TextField(searchTerm);
                string lowerSearchTerm = searchTerm.ToLower();
                SerializedObject choseProxy = new SerializedObject(choosingFromComponent);
                SerializedProperty iter = choseProxy.GetIterator();
                if (iter.Next(true))
                    do
                        if (propertyTypeIndexMap.ContainsKey(iter.propertyType)
                            && iter.name.ToLower().Contains(lowerSearchTerm)
                            && GUILayout.Button(new GUIContent(iter.name)))
                        {
                            propertyName = iter.name;
                            selectedPropertyTypeIndex = propertyTypeIndexMap[iter.propertyType];
                            SetValueFromProperty(iter);
                        }
                    while (iter.Next(false));
            }

            EditorGUILayout.Separator();

            SerializedObject proxy = new SerializedObject(this);
            EditorGUILayout.PropertyField(proxy.FindProperty(nameof(componentName)));
            EditorGUILayout.PropertyField(proxy.FindProperty(nameof(propertyName)));
            PropertyValueEditors(proxy);
            proxy.ApplyModifiedProperties();

            EditorGUILayout.Separator();

            if (GUILayout.Button(new GUIContent("Search And Select")))
                SearchAndSelect();
            if (GUILayout.Button(new GUIContent("Search Into Selection Stage")))
                SearchIntoSelectionStage();

            if (foundCount != -1)
            {
                EditorGUILayout.Separator();
                GUILayout.Label($"Found Count: {foundCount}");
            }

            EditorGUILayout.EndScrollView();
        }

        private void SetValueFromProperty(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    longValue = property.longValue;
                    break;
                case SerializedPropertyType.Boolean:
                    boolValue = property.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    doubleValue = property.doubleValue;
                    break;
                case SerializedPropertyType.String:
                    stringValue = property.stringValue;
                    break;
                case SerializedPropertyType.Color:
                    colorValue = property.colorValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    objectReferenceValue = property.objectReferenceValue;
                    break;
                case SerializedPropertyType.LayerMask:
                    layerMaskValue = property.intValue;
                    break;
                // case SerializedPropertyType.Enum: // this is wrong, enums would require more special handling.
                //     enumValue = property.longValue;
                //     break;
                case SerializedPropertyType.Vector2:
                    vector2Value = property.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    vector3Value = property.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    vector4Value = property.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    rectValue = property.rectValue;
                    break;
                case SerializedPropertyType.Character:
                    characterValue = property.stringValue;
                    break;
                case SerializedPropertyType.Bounds:
                    boundsValue = property.boundsValue;
                    break;
                case SerializedPropertyType.Gradient:
                    // gradientValue = property.; // TODO: how do you get the gradient value?
                    break;
                case SerializedPropertyType.Quaternion:
                    quaternionValue = property.quaternionValue;
                    break;
                case SerializedPropertyType.Vector2Int:
                    vector2IntValue = property.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    vector3IntValue = property.vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    rectIntValue = property.rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    boundsIntValue = property.boundsIntValue;
                    break;
            }
        }

        private void PropertyValueEditors(SerializedObject proxy)
        {
            selectedPropertyTypeIndex = EditorGUILayout.Popup(new GUIContent("Property Type"), selectedPropertyTypeIndex, propertyTypeStrings);
            switch (propertyTypes[selectedPropertyTypeIndex])
            {
                case SerializedPropertyType.Integer:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(longValue)));
                    break;
                case SerializedPropertyType.Boolean:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(boolValue)));
                    break;
                case SerializedPropertyType.Float:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(doubleValue)));
                    break;
                case SerializedPropertyType.String:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(stringValue)));
                    break;
                case SerializedPropertyType.Color:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(colorValue)));
                    break;
                case SerializedPropertyType.ObjectReference:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(objectReferenceValue)));
                    break;
                case SerializedPropertyType.LayerMask:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(layerMaskValue)));
                    break;
                // case SerializedPropertyType.Enum: // this is wrong, enums would require more special handling.
                //     EditorGUILayout.PropertyField(proxy.FindProperty(nameof(enumValue)));
                //     break;
                case SerializedPropertyType.Vector2:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(vector2Value)));
                    break;
                case SerializedPropertyType.Vector3:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(vector3Value)));
                    break;
                case SerializedPropertyType.Vector4:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(vector4Value)));
                    break;
                case SerializedPropertyType.Rect:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(rectValue)));
                    break;
                case SerializedPropertyType.Character:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(characterValue)));
                    break;
                case SerializedPropertyType.Bounds:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(boundsValue)));
                    break;
                case SerializedPropertyType.Gradient:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(gradientValue)));
                    break;
                case SerializedPropertyType.Quaternion:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(quaternionValue)));
                    break;
                case SerializedPropertyType.Vector2Int:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(vector2IntValue)));
                    break;
                case SerializedPropertyType.Vector3Int:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(vector3IntValue)));
                    break;
                case SerializedPropertyType.RectInt:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(rectIntValue)));
                    break;
                case SerializedPropertyType.BoundsInt:
                    EditorGUILayout.PropertyField(proxy.FindProperty(nameof(boundsIntValue)));
                    break;
                default:
                    break;
            }
        }

        private void SearchAndSelect()
        {
            Selection.objects = Search().ToArray();
        }

        private void SearchIntoSelectionStage()
        {
            ICollection<GameObject> results = Search();
            if (results.Count == 0)
                return;
            if (selectionStage == null)
            {
                selectionStage = CreateWindow<SelectionStageWindow>(typeof(PropertySearchWindow));
                selectionStage.titleContent = new GUIContent("Results Stage");
            }
            selectionStage.SetStage(results);
            selectionStage.Show();
            selectionStage.Focus();
        }

        private HashSet<GameObject> Search()
        {
            HashSet<GameObject> toSelect = new HashSet<GameObject>();
            System.Type componentType = typeof(Component);
            foreach (Component component in UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Component>())
                .Where(c => c != null && c.GetType().Name == componentName && !toSelect.Contains(c.gameObject)))
            {
                SerializedObject componentProxy = new SerializedObject(component);
                SerializedProperty property = componentProxy.FindProperty(propertyName);
                if (property == null)
                    continue;
                if (propertyTypes[selectedPropertyTypeIndex] != property.propertyType)
                    continue;
                if (!ComparePropertyValue(property))
                    continue;
                // Found a match!
                toSelect.Add(component.gameObject);
            }
            foundCount = toSelect.Count;
            return toSelect;
        }

        private bool ComparePropertyValue(SerializedProperty property)
        {
            switch (propertyTypes[selectedPropertyTypeIndex])
            {
                case SerializedPropertyType.Integer:
                    return property.longValue == longValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue == boolValue;
                case SerializedPropertyType.Float:
                    double propertyValue = property.doubleValue;
                    if (double.IsNaN(propertyValue))
                        return double.IsNaN(doubleValue);
                    if (propertyValue == doubleValue) // Handle infinity, or actually equal values.
                        return true;
                    // This is absolutely required because the actual backing value for a doubleValue
                    // of a property may either be a float or a double. If it is a float then
                    // converting it to double can easily end up not being equal to the double value
                    // when the given decimal value was not possible to be represented using floats.
                    return doubleValue - 0.0000005d <= propertyValue && propertyValue <= doubleValue + 0.0000005d;
                case SerializedPropertyType.String:
                    return property.stringValue == stringValue;
                case SerializedPropertyType.Color:
                    return property.colorValue == colorValue;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue == objectReferenceValue;
                case SerializedPropertyType.LayerMask:
                    return property.intValue == layerMaskValue;
                // case SerializedPropertyType.Enum:
                //     return property.enum;
                case SerializedPropertyType.Vector2:
                    return property.vector2Value == vector2Value;
                case SerializedPropertyType.Vector3:
                    return property.vector3Value == vector3Value;
                case SerializedPropertyType.Vector4:
                    return property.vector4Value == vector4Value;
                case SerializedPropertyType.Rect:
                    return property.rectValue == rectValue;
                case SerializedPropertyType.Character:
                    return property.stringValue == characterValue;
                case SerializedPropertyType.Bounds:
                    return property.boundsValue == boundsValue;
                // case SerializedPropertyType.Gradient: // How to get the gradient value?
                //     return property.;
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue == quaternionValue;
                case SerializedPropertyType.Vector2Int:
                    return property.vector2IntValue == vector2IntValue;
                case SerializedPropertyType.Vector3Int:
                    return property.vector3IntValue == vector3IntValue;
                case SerializedPropertyType.RectInt:
                    return property.rectIntValue.Equals(rectIntValue); // Idk why RectInt doesn't have ==, but Equals should work.
                case SerializedPropertyType.BoundsInt:
                    return property.boundsIntValue == boundsIntValue;
                default:
                    return false;
            }
        }
    }
}
