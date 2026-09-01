using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-off editor tooling: applies the reference CapsuleCollider + kinematic Rigidbody
/// (tuned on Mystic_1_Star1) to every unit prefab so clicks hit units directly.
/// </summary>
public static class UnitColliderTool
{
    private const string UnitsRoot   = "Assets/Resources/Prefabs/Units";
    private const string ReferencePath = "Assets/Resources/Prefabs/Units/Mystic_Units/Star1/Mystic_1_Star1.prefab";

    // Exact settings copied from Mystic_1_Star1.
    private const float   Radius = 0.5f;
    private const float   Height = 2.4f;
    private static readonly Vector3 Center = new Vector3(0.018882364f, 1.0395093f, 0.058097035f);

    [MenuItem("Tools/Units/Apply Collider To All Units")]
    public static void ApplyMenu() => Debug.Log(ApplyToAll());

    public static string ApplyToAll()
    {
        var sb = new StringBuilder();
        int changed = 0, skippedNoUnit = 0, skippedRef = 0;

        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { UnitsRoot });
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            if (path == ReferencePath) { skippedRef++; continue; } // leave the reference prefab untouched

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<UnitController>() == null) { skippedNoUnit++; continue; }

                var cap = root.GetComponent<CapsuleCollider>();
                if (cap == null) cap = root.AddComponent<CapsuleCollider>();
                cap.direction = 1; // Y axis
                cap.radius    = Radius;
                cap.height    = Height;
                cap.center    = Center;
                cap.isTrigger = false;

                var rb = root.GetComponent<Rigidbody>();
                if (rb == null) rb = root.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity  = false;

                PrefabUtility.SaveAsPrefabAsset(root, path);
                changed++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        AssetDatabase.SaveAssets();
        return $"[UnitColliderTool] changed={changed} skippedRef={skippedRef} skippedNoUnit={skippedNoUnit}";
    }
}
