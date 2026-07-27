using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates Star2 unit prefabs from Star1 by applying the standard star-up recipe.
/// Editor-only tooling driven through unity-cli.
/// </summary>
public static class StarUpGenerator
{
    private const string GeoPath   = "Visual/Geometry/geo"; // objects to enable + reskin live under here
    private const float   ScaleMul = 1.1f;                  // root scale multiplier
    private static readonly Vector3 ShieldScale = new Vector3(0.6f, 0.6f, 0.6f);
    private const string SilverMatPath = "Assets/Resources/Materials/Others/Silver.mat";

    // Shared US_Soldier armor pieces to enable on star-up (helmet/holster stay off, per Mystic).
    private static readonly HashSet<string> EnableObjects = new HashSet<string>
    {
        "SK_Backpack", "SK_Belly_Pouches", "SK_Belt", "SK_Chest_Pouches",
        "SK_Elbow_Guard_L", "SK_Elbow_Guard_R", "SK_Leg_Guard_L", "SK_Leg_Guard_R",
        "SK_Shoulder_Pouches",
    };

    // Material slots on the enabled pieces that become Silver (metal/main slots only).
    private static readonly HashSet<string> SilverSlots = new HashSet<string>
    {
        "M_Backpack", "M_Pouch", "M_Elbow_Pad_Metal", "M_Leg_Guard_Metal",
    };

    private static readonly string[] TargetFactions =
    {
        "Assets/Resources/Prefabs/Units/Chaos_Units",
        "Assets/Resources/Prefabs/Units/Divinity_Units",
        "Assets/Resources/Prefabs/Units/Heretic_Units",
        "Assets/Resources/Prefabs/Units/Innovation_Units",
    };

    // Star3 recipe //
    private const float   Star3Scale     = 1.2f;
    private static readonly Vector3 Star3Shield = new Vector3(0.65f, 0.65f, 0.65f);
    private const string GoldMatPath = "Assets/Resources/Materials/Others/Gold.mat";
    private const string UniqueColorObject = "SK_Mask";   // its material is the unit's unique accent color
    private const string HelmetColorSlot   = "M_Helmet";  // helmet slot that takes the unique color

    // Metal/main slots that become Gold on Star3 (Silver from Star2 also -> Gold, handled separately).
    private static readonly HashSet<string> GoldSlots = new HashSet<string>
    {
        "M_Backpack", "M_Pouch", "M_Elbow_Pad_Metal", "M_Leg_Guard_Metal", "M_Helmet_Frame",
    };

    // All five factions carry Star2 folders to promote to Star3.
    private static readonly string[] Star3Factions =
    {
        "Assets/Resources/Prefabs/Units/Mystic_Units",
        "Assets/Resources/Prefabs/Units/Chaos_Units",
        "Assets/Resources/Prefabs/Units/Divinity_Units",
        "Assets/Resources/Prefabs/Units/Heretic_Units",
        "Assets/Resources/Prefabs/Units/Innovation_Units",
    };


    // Batch driver //

    /// <summary>Standardize folders and generate Star2 for every remaining unit across target factions.</summary>
    public static string GenerateAll()
    {
        Material silver = AssetDatabase.LoadAssetAtPath<Material>(SilverMatPath);
        if (silver == null) return $"[Abort] Silver material not found at {SilverMatPath}";

        var sb = new StringBuilder();
        int made = 0;
        foreach (string faction in TargetFactions)
        {
            string star1Dir = faction + "/Star1";
            string star2Dir = faction + "/Star2";
            if (!AssetDatabase.IsValidFolder(star1Dir)) AssetDatabase.CreateFolder(faction, "Star1");
            if (!AssetDatabase.IsValidFolder(star2Dir)) AssetDatabase.CreateFolder(faction, "Star2");

            // Collect Star1 prefabs anywhere under the faction folder.
            var star1Paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { faction }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("_Star1.prefab")) star1Paths.Add(p);
            }
            star1Paths.Sort();

            foreach (string loose in star1Paths)
            {
                string name = Path.GetFileName(loose);
                string desired = star1Dir + "/" + name;
                string finalStar1 = loose;
                if (loose != desired) // move loose Star1 into the Star1/ subfolder (folder separation)
                {
                    string err = AssetDatabase.MoveAsset(loose, desired);
                    if (!string.IsNullOrEmpty(err)) { sb.AppendLine($"[MoveFail] {loose}: {err}"); continue; }
                    finalStar1 = desired;
                }

                string star2Path = star2Dir + "/" + name.Replace("_Star1.prefab", "_Star2.prefab");
                sb.AppendLine(GenerateOne(finalStar1, star2Path, silver));
                made++;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        sb.Insert(0, $"[GenerateAll] {made} Star2 prefabs generated\n");
        return sb.ToString();
    }

    /// <summary>Copy a Star1 prefab to star2Path and apply the star-up recipe. Returns a one-line report.</summary>
    public static string GenerateOne(string star1Path, string star2Path, Material silver)
    {
        if (silver == null) silver = AssetDatabase.LoadAssetAtPath<Material>(SilverMatPath);
        if (!AssetDatabase.CopyAsset(star1Path, star2Path)) return $"[CopyFail] {star1Path} -> {star2Path}";

        GameObject root = PrefabUtility.LoadPrefabContents(star2Path);
        try
        {
            root.name = Path.GetFileNameWithoutExtension(star2Path);
            root.transform.localScale = root.transform.localScale * ScaleMul;

            UnitVisuals vis = root.GetComponentInChildren<UnitVisuals>(true);
            if (vis != null)
            {
                var so = new SerializedObject(vis);
                SerializedProperty sp = so.FindProperty("shieldScale");
                if (sp != null) { sp.vector3Value = ShieldScale; so.ApplyModifiedPropertiesWithoutUndo(); }
            }

            int enabled = 0, swapped = 0;
            Transform geo = root.transform.Find(GeoPath);
            if (geo != null)
            {
                foreach (string objName in EnableObjects)
                {
                    Transform piece = geo.Find(objName);
                    if (piece == null) continue;
                    piece.gameObject.SetActive(true);
                    enabled++;

                    var r = piece.GetComponent<Renderer>();
                    if (r == null) continue;
                    Material[] mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                        if (mats[i] != null && SilverSlots.Contains(mats[i].name)) { mats[i] = silver; swapped++; }
                    r.sharedMaterials = mats;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, star2Path);
            return $"[OK] {Path.GetFileName(star2Path)}  scale={root.transform.localScale.x:0.##} enabled={enabled} silvered={swapped}"
                 + (geo == null ? "  <NO geo!>" : "");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Star3 driver //

    /// <summary>Generate Star3 from every Star2 across all factions (skips any Star3 that already exists).</summary>
    public static string GenerateAllStar3()
    {
        Material gold = AssetDatabase.LoadAssetAtPath<Material>(GoldMatPath);
        if (gold == null) return $"[Abort] Gold material not found at {GoldMatPath}";

        var sb = new StringBuilder();
        int made = 0, skipped = 0;
        foreach (string faction in Star3Factions)
        {
            string star2Dir = faction + "/Star2";
            string star3Dir = faction + "/Star3";
            if (!AssetDatabase.IsValidFolder(star2Dir)) continue;
            if (!AssetDatabase.IsValidFolder(star3Dir)) AssetDatabase.CreateFolder(faction, "Star3");

            var star2Paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { star2Dir }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("_Star2.prefab")) star2Paths.Add(p);
            }
            star2Paths.Sort();

            foreach (string star2 in star2Paths)
            {
                string name = Path.GetFileName(star2);
                string star3Path = star3Dir + "/" + name.Replace("_Star2.prefab", "_Star3.prefab");
                if (AssetDatabase.LoadAssetAtPath<GameObject>(star3Path) != null) { skipped++; continue; } // keep user's reference
                sb.AppendLine(GenerateStar3One(star2, star3Path, gold));
                made++;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        sb.Insert(0, $"[GenerateAllStar3] {made} generated, {skipped} kept (already existed)\n");
        return sb.ToString();
    }

    /// <summary>Copy a Star2 prefab to star3Path and apply the Star3 recipe. Returns a one-line report.</summary>
    public static string GenerateStar3One(string star2Path, string star3Path, Material gold)
    {
        if (gold == null) gold = AssetDatabase.LoadAssetAtPath<Material>(GoldMatPath);
        if (!AssetDatabase.CopyAsset(star2Path, star3Path)) return $"[CopyFail] {star2Path} -> {star3Path}";

        GameObject root = PrefabUtility.LoadPrefabContents(star3Path);
        try
        {
            root.name = Path.GetFileNameWithoutExtension(star3Path);
            root.transform.localScale = Vector3.one * Star3Scale;

            UnitVisuals vis = root.GetComponentInChildren<UnitVisuals>(true);
            if (vis != null)
            {
                var so = new SerializedObject(vis);
                SerializedProperty sp = so.FindProperty("shieldScale");
                if (sp != null) { sp.vector3Value = Star3Shield; so.ApplyModifiedPropertiesWithoutUndo(); }
            }

            int enabled = 0, gilded = 0;
            Transform geo = root.transform.Find(GeoPath);
            if (geo != null)
            {
                // Unit's unique accent color = the material on SK_Mask.
                Material unique = null;
                Transform mask = geo.Find(UniqueColorObject);
                if (mask != null)
                {
                    var mr = mask.GetComponent<Renderer>();
                    if (mr != null && mr.sharedMaterials.Length > 0) unique = mr.sharedMaterials[0];
                }

                foreach (Transform piece in geo)
                {
                    if (!piece.gameObject.activeSelf) { piece.gameObject.SetActive(true); enabled++; }

                    var r = piece.GetComponent<Renderer>();
                    if (r == null) continue;
                    Material[] mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null) continue;
                        string mn = mats[i].name;
                        if (mn == "Silver" || GoldSlots.Contains(mn)) { mats[i] = gold; gilded++; }
                        else if (mn == HelmetColorSlot && unique != null) { mats[i] = unique; }
                    }
                    r.sharedMaterials = mats;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, star3Path);
            return $"[OK] {Path.GetFileName(star3Path)}  scale={root.transform.localScale.x:0.##} enabled+={enabled} gold={gilded}"
                 + (geo == null ? "  <NO geo!>" : "");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }


    // Star3 weapon gold patch //

    /// <summary>Swap Silver->Gold on every object under GunSlot for all Star3 prefabs (skips Mystic_1, already done).</summary>
    public static string PatchStar3Weapons()
    {
        Material gold = AssetDatabase.LoadAssetAtPath<Material>(GoldMatPath);
        if (gold == null) return $"[Abort] Gold material not found at {GoldMatPath}";

        var sb = new StringBuilder();
        int patched = 0, swaps = 0;
        foreach (string faction in Star3Factions)
        {
            string star3Dir = faction + "/Star3";
            if (!AssetDatabase.IsValidFolder(star3Dir)) continue;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { star3Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("_Star3.prefab")) continue;
                if (path.EndsWith("Mystic_1_Star3.prefab")) continue; // user's reference, already gold

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Transform gun = null;
                    foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                        if (t.name.Contains("GunSlot")) { gun = t; break; }

                    int n = 0;
                    if (gun != null)
                        foreach (Renderer r in gun.GetComponentsInChildren<Renderer>(true))
                        {
                            Material[] mats = r.sharedMaterials;
                            bool changed = false;
                            for (int i = 0; i < mats.Length; i++)
                                if (mats[i] != null && mats[i].name == "Silver") { mats[i] = gold; changed = true; n++; }
                            if (changed) r.sharedMaterials = mats;
                        }

                    if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                    sb.AppendLine($"[{(gun == null ? "NoGunSlot" : "OK")}] {Path.GetFileName(path)} silver->gold={n}");
                    patched += n > 0 ? 1 : 0;
                    swaps += n;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        sb.Insert(0, $"[PatchStar3Weapons] {patched} prefabs changed, {swaps} slots silver->gold\n");
        return sb.ToString();
    }


    // UnitData wiring //

    private static readonly string[] DataFactions =
    {
        "Assets/Data/Units/Mystic",
        "Assets/Data/Units/Chaos",
        "Assets/Data/Units/Divinity",
        "Assets/Data/Units/Heretic",
        "Assets/Data/Units/Innovation",
    };

    private const float Star3StatMul = 1.7f;

    /// <summary>Round to 1 decimal (half away from zero) via decimal to avoid float drift (e.g. 59.5*1.7 -> 101.2).</summary>
    private static float Round1(float v)
        => (float)System.Math.Round((decimal)v * (decimal)Star3StatMul, 1, System.MidpointRounding.AwayFromZero);

    /// <summary>
    /// Point Star2 unitPrefab at the Star2 prefab; create/fill Star3 UnitData (stats x1.7 of Star2,
    /// starLevel 3, unitPrefab -> Star3 prefab); wire Star2.upgradeUnit -> Star3.
    /// </summary>
    public static string WireStarData()
    {
        var sb = new StringBuilder();
        int wired = 0, created = 0, failed = 0;

        foreach (string dataDir in DataFactions)
        {
            string star2Dir = dataDir + "/Star2";
            string star3Dir = dataDir + "/Star3";
            if (!AssetDatabase.IsValidFolder(star2Dir)) continue;
            if (!AssetDatabase.IsValidFolder(star3Dir)) AssetDatabase.CreateFolder(dataDir, "Star3");

            foreach (string guid in AssetDatabase.FindAssets("t:UnitData", new[] { star2Dir }))
            {
                string star2Path = AssetDatabase.GUIDToAssetPath(guid);
                if (!star2Path.EndsWith("_Star2.asset")) continue;
                UnitData star2 = AssetDatabase.LoadAssetAtPath<UnitData>(star2Path);
                if (star2 == null) { sb.AppendLine($"[Skip] load fail {star2Path}"); failed++; continue; }

                // Reliable Star1 prefab path: prefer the Star1 data's reference, else Star2's own.
                string star1DataPath = star2Path.Replace("/Star2/", "/Star1/").Replace("_Star2.asset", "_Star1.asset");
                UnitData star1 = AssetDatabase.LoadAssetAtPath<UnitData>(star1DataPath);
                string p1 = star1 != null && star1.unitPrefab != null ? AssetDatabase.GetAssetPath(star1.unitPrefab) : null;
                if (string.IsNullOrEmpty(p1) || !p1.Contains("/Star1/"))
                    p1 = star2.unitPrefab != null ? AssetDatabase.GetAssetPath(star2.unitPrefab) : null;
                if (string.IsNullOrEmpty(p1) || !p1.Contains("/Star1/"))
                { sb.AppendLine($"[Fail] no Star1 prefab for {Path.GetFileName(star2Path)}"); failed++; continue; }

                string p2 = p1.Replace("Star1", "Star2");
                string p3 = p1.Replace("Star1", "Star3");
                var star2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p2);
                var star3Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p3);
                if (star2Prefab == null || star3Prefab == null)
                { sb.AppendLine($"[Fail] prefab missing s2={(star2Prefab!=null)} s3={(star3Prefab!=null)} for {Path.GetFileName(star2Path)}"); failed++; continue; }

                // 1) Star2 -> Star2 prefab
                star2.unitPrefab = star2Prefab;
                star2.starLevel = 2;

                // 2) Star3 data (create from Star2 if absent), scale x1.7, wire prefab
                string star3Path = star2Path.Replace("/Star2/", "/Star3/").Replace("_Star2.asset", "_Star3.asset");
                if (AssetDatabase.LoadAssetAtPath<UnitData>(star3Path) == null)
                { AssetDatabase.CopyAsset(star2Path, star3Path); created++; }
                UnitData star3 = AssetDatabase.LoadAssetAtPath<UnitData>(star3Path);
                if (star3 == null) { sb.AppendLine($"[Fail] star3 load {star3Path}"); failed++; continue; }

                star3.starLevel = 3;
                star3.upgradeUnit = null;
                star3.unitPrefab = star3Prefab;
                star3.maxHp = Round1(star2.maxHp); // x1.7 of Star2
                star3.att   = Round1(star2.att);

                // 3) Star2 -> Star3
                star2.upgradeUnit = star3;

                EditorUtility.SetDirty(star2);
                EditorUtility.SetDirty(star3);
                sb.AppendLine($"[OK] {Path.GetFileName(star2Path)}: prefab->Star2, up->{star3.name}  Star3 maxHp {star2.maxHp}->{star3.maxHp} att {star2.att}->{star3.att}");
                wired++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        sb.Insert(0, $"[WireStarData] {wired} wired, {created} Star3 created, {failed} failed\n");
        return sb.ToString();
    }


    // Inspection //

    /// <summary>Dump the geo subtree (active state + materials) of a prefab for verifying the recipe.</summary>
    public static string Inspect(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[Inspect] {prefabPath}");
            sb.AppendLine($"  root localScale = {root.transform.localScale}");

            UnitVisuals vis = root.GetComponentInChildren<UnitVisuals>(true);
            if (vis != null)
            {
                var so = new SerializedObject(vis);
                SerializedProperty sp = so.FindProperty("shieldScale");
                sb.AppendLine($"  shieldScale = {(sp != null ? sp.vector3Value.ToString() : "<no field>")}");
            }
            else sb.AppendLine("  <no UnitVisuals>");

            Transform geo = root.transform.Find(GeoPath);
            if (geo == null) sb.AppendLine($"  <no '{GeoPath}'>");
            else
            {
                sb.AppendLine($"  geo children = {geo.childCount}");
                DumpTree(geo, sb, 2);
            }
            return sb.ToString();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    /// <summary>Find the first transform whose name contains <paramref name="needle"/> and dump its subtree with materials.</summary>
    public static string InspectByName(string prefabPath, string needle)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform hit = null;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.Contains(needle)) { hit = t; break; }

            var sb = new StringBuilder();
            sb.AppendLine($"[InspectByName '{needle}'] {prefabPath}");
            if (hit == null) { sb.AppendLine("  <not found>"); return sb.ToString(); }
            sb.AppendLine($"  {hit.name} active={hit.gameObject.activeSelf}");
            DumpTree(hit, sb, 2);
            return sb.ToString();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void DumpTree(Transform t, StringBuilder sb, int indent)
    {
        foreach (Transform c in t)
        {
            string pad = new string(' ', indent * 2);
            var r = c.GetComponent<Renderer>();
            string mats = "-";
            if (r != null)
            {
                var names = new List<string>();
                foreach (Material m in r.sharedMaterials) names.Add(m != null ? m.name : "null");
                mats = string.Join(",", names);
            }
            sb.AppendLine($"{pad}{c.name} active={c.gameObject.activeSelf} mats=[{mats}]");
            DumpTree(c, sb, indent + 1);
        }
    }
}
