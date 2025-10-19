using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public class ForceReserializer : MonoBehaviour
    {
        [MenuItem("Tools/LM Pro/Force Reserialize Assets")]
        private static void UpdateGroundMaterials()
        {
            AssetDatabase.ForceReserializeAssets();
        }
    }
}