using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;

namespace JanSharp
{
    public static class LogComponentClassNames
    {
        [MenuItem("Tools/JanSharp/Log Component Class Names", isValidateFunction: true, priority: 1000)]
        public static bool ValidateDoLogComponentClassNames()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("Tools/JanSharp/Log Component Class Names", isValidateFunction: false, priority: 1000)]
        public static void DoLogComponentClassNames()
        {
            string components = string.Join(", ", Selection.activeGameObject.GetComponents<Component>()
                .Select(c => c == null ? "<missing-script>" : c.GetType().Name));
            Debug.Log("Component Class Names: " + components, Selection.activeGameObject);
        }
    }
}
