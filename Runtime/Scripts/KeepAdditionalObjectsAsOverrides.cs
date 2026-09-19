using UnityEngine;

namespace JanSharp
{
    public class KeepAdditionalObjectsAsOverrides : MonoBehaviour
    {
        [Tooltip("Leave the list empty to match any objects")]
        public string[] regularExpressions;
    }
}
