using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;

namespace JanSharp
{
    public static class PrintComponentClassNames
    {
        [MenuItem("Tools/JanSharp/Print Component Class Names", isValidateFunction: true, priority: 1000)]
        public static bool ValidateDoPrintComponentClassNames()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("Tools/JanSharp/Print Component Class Names", isValidateFunction: false, priority: 1000)]
        public static void DoPrintComponentClassNames()
        {
            string components = string.Join(", ", Selection.activeGameObject.GetComponents<Component>()
                .Select(c => c == null ? "<missing-script>" : c.GetType().Name));
            Debug.Log("Component Class Names: " + components, Selection.activeGameObject);
        }
    }
}
