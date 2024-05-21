using UnityEngine;
using UnityEditor;

namespace JanSharp
{
    public static class PrintSelectedCount
    {
        [MenuItem("Tools/JanSharp/Print Selected Count", isValidateFunction: true, priority = 1000)]
        public static bool ValidateDoPrintSelectedCount()
        {
            return Selection.activeObject != null;
        }

        [MenuItem("Tools/JanSharp/Print Selected Count", priority = 1000)]
        public static void DoPrintSelectedCount()
        {
            Debug.Log("Selected Count: " + Selection.objects.Length);
        }
    }
}
