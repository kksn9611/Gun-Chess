using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-off editor tooling: makes the synergy badge hoverable and builds the shared tooltip panel.
/// </summary>
public static class SynergyTooltipBuilder
{
    private const string BadgePath = "Assets/Resources/Prefabs/UI/SynergyBadge.prefab";
    private const string ThumbPrefabPath = "Assets/Resources/Prefabs/UI/SynergyThumbnail.prefab";
    private const string DatabasePath = "Assets/Data/Units/UnitPoolDatabase.asset";
    private const string PalettePath = "Assets/Data/ShopCostPalette.asset";

    // Give the thumbnail cell a cost-colored border ring (root = border, inset Icon = thumbnail). //
    public static string AddThumbnailBorder()
    {
        var root = PrefabUtility.LoadPrefabContents(ThumbPrefabPath);
        try
        {
            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var rootImg = root.GetComponent<Image>();

            // Inner Icon holds the thumbnail/placeholder, inset so the root shows as a border.
            Transform iconT = root.transform.Find("Icon");
            Image iconImg;
            if (iconT == null)
            {
                var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(root.transform, false);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(2.5f, 2.5f); rt.offsetMax = new Vector2(-2.5f, -2.5f);
                iconImg = go.GetComponent<Image>();
                iconImg.sprite = rootImg != null ? rootImg.sprite : rounded;
                iconImg.type = Image.Type.Sliced;
                iconImg.color = new Color(0.30f, 0.30f, 0.30f, 1f); // placeholder look
                iconImg.raycastTarget = false;
            }
            else iconImg = iconT.GetComponent<Image>();

            // Root becomes the border (tinted by cost at runtime).
            if (rootImg != null) { rootImg.sprite = rounded; rootImg.type = Image.Type.Sliced; rootImg.color = Color.white; }

            var th = root.GetComponent<SynergyThumbnail>();
            var so = new SerializedObject(th);
            so.FindProperty("image").objectReferenceValue   = iconImg;
            so.FindProperty("border").objectReferenceValue  = rootImg;
            so.FindProperty("palette").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ShopCostPalette>(PalettePath);
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ThumbPrefabPath);
            return "[AddThumbnailBorder] Icon inset + border wired, palette=" + (so.FindProperty("palette").objectReferenceValue != null);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Thumbnail cell prefab: square gray Image + SynergyThumbnail. //
    public static string BuildThumbnailCell()
    {
        var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var root = new GameObject("SynergyThumbnail", typeof(RectTransform), typeof(Image), typeof(SynergyThumbnail));
        ((RectTransform)root.transform).sizeDelta = new Vector2(40f, 40f);
        var img = root.GetComponent<Image>();
        img.sprite = rounded; img.type = Image.Type.Sliced;
        img.color = new Color(0.30f, 0.30f, 0.30f, 1f); // placeholder look when no portrait

        var so = new SerializedObject(root.GetComponent<SynergyThumbnail>());
        so.FindProperty("image").objectReferenceValue = img;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, ThumbPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "[BuildThumbnailCell] saved " + ThumbPrefabPath;
    }

    // Build the unit tooltip, drop the static placeholders, wire SynergyTooltip. //
    public static string SetupThumbnailHover()
    {
        var ui = Object.FindFirstObjectByType<SynergyUI>();
        if (ui == null) return "[SetupThumbnailHover] no SynergyUI";
        var canvas = ui.GetComponentInParent<Canvas>();
        var tipT = canvas.transform.Find("SynergyTooltip");
        if (tipT == null) return "[SetupThumbnailHover] no SynergyTooltip";
        var tip = tipT.GetComponent<SynergyTooltip>();
        var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Remove the static placeholder cells — thumbnails are populated dynamically now.
        var thumbs = tipT.Find("Thumbnails");
        if (thumbs != null)
            for (int i = thumbs.childCount - 1; i >= 0; i--)
            {
                var c = thumbs.GetChild(i);
                if (c.name.StartsWith("Placeholder")) Object.DestroyImmediate(c.gameObject);
            }

        // Unit tooltip panel (name + synergy names)
        UnitTooltip unitTip;
        var utT = canvas.transform.Find("UnitTooltip");
        if (utT == null)
        {
            var go = new GameObject("UnitTooltip", typeof(RectTransform), typeof(Image),
                                    typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(UnitTooltip));
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvas.transform, false); rt.SetAsLastSibling(); // on top of the synergy tooltip
            rt.pivot = new Vector2(0.5f, 0f); rt.sizeDelta = new Vector2(200f, 80f);

            var bg = go.GetComponent<Image>();
            bg.sprite = rounded; bg.type = Image.Type.Sliced; bg.color = new Color(0f, 0f, 0f, 0.9f); bg.raycastTarget = false;

            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 8, 8); vlg.spacing = 4f; vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;

            var csf = go.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(go.transform, false);
            var nt = nameGo.GetComponent<TextMeshProUGUI>();
            nt.text = "Unit"; nt.fontSize = 18f; nt.fontStyle = FontStyles.Bold;

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGo.transform.SetParent(go.transform, false);
            var bt = bodyGo.GetComponent<TextMeshProUGUI>();
            bt.text = "Synergies"; bt.fontSize = 14f; bt.enableWordWrapping = true; bt.color = new Color(0.8f, 0.8f, 0.8f, 1f);

            unitTip = go.GetComponent<UnitTooltip>();
            var uso = new SerializedObject(unitTip);
            uso.FindProperty("panel").objectReferenceValue    = rt;
            uso.FindProperty("nameText").objectReferenceValue = nt;
            uso.FindProperty("bodyText").objectReferenceValue = bt;
            uso.ApplyModifiedPropertiesWithoutUndo();
        }
        else unitTip = utT.GetComponent<UnitTooltip>();

        // Wire SynergyTooltip refs
        var db = AssetDatabase.LoadAssetAtPath<UnitPoolDatabase>(DatabasePath);
        var cell = AssetDatabase.LoadAssetAtPath<SynergyThumbnail>(ThumbPrefabPath);
        var so = new SerializedObject(tip);
        so.FindProperty("database").objectReferenceValue           = db;
        so.FindProperty("thumbnailCellPrefab").objectReferenceValue = cell;
        so.FindProperty("unitTooltip").objectReferenceValue         = unitTip;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tip);

        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
        return $"[SetupThumbnailHover] db={(db != null)} cell={(cell != null)} unitTip={(unitTip != null)}";
    }

    // Add a transparent full-row raycast target so the whole badge receives hover. //
    public static string AddBadgeRaycast()
    {
        var root = PrefabUtility.LoadPrefabContents(BadgePath);
        try
        {
            var img = root.GetComponent<Image>();
            if (img == null) img = root.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f); // invisible, still raycasts
            img.raycastTarget = true;
            PrefabUtility.SaveAsPrefabAsset(root, BadgePath);
            return "[AddBadgeRaycast] root raycast image added";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Add a bottom thumbnail row (square cells, wraps) with placeholder cells. //
    public static string AddThumbnails()
    {
        var ui = Object.FindFirstObjectByType<SynergyUI>();
        if (ui == null) return "[AddThumbnails] no SynergyUI in scene";
        var canvas = ui.GetComponentInParent<Canvas>();
        if (canvas == null) return "[AddThumbnails] no Canvas";
        var tipT = canvas.transform.Find("SynergyTooltip");
        if (tipT == null) return "[AddThumbnails] no SynergyTooltip (run BuildTooltip first)";
        var tip = tipT.GetComponent<SynergyTooltip>();

        var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        Transform thumbsT = tipT.Find("Thumbnails");
        RectTransform thumbs;
        if (thumbsT == null)
        {
            var go = new GameObject("Thumbnails", typeof(RectTransform), typeof(GridLayoutGroup));
            thumbs = (RectTransform)go.transform;
            thumbs.SetParent(tipT, false);
            thumbs.SetAsLastSibling(); // very bottom of the tooltip

            var grid = go.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(40f, 40f);   // square cells
            grid.spacing = new Vector2(4f, 4f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible; // wraps by width

            // Placeholder square cells — delete these when you populate dynamically.
            for (int i = 0; i < 4; i++)
            {
                var cell = new GameObject("Placeholder" + i, typeof(RectTransform), typeof(Image));
                cell.transform.SetParent(thumbs, false);
                var img = cell.GetComponent<Image>();
                img.sprite = rounded; img.type = Image.Type.Sliced;
                img.color = new Color(0.30f, 0.30f, 0.30f, 1f);
                img.raycastTarget = false;
            }
        }
        else thumbs = (RectTransform)thumbsT;

        var so = new SerializedObject(tip);
        so.FindProperty("thumbnailContainer").objectReferenceValue = thumbs;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tip);

        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
        return "[AddThumbnails] Thumbnails grid added at bottom, wired thumbnailContainer";
    }

    // Build the tooltip panel under the SynergyUI canvas and wire everything. //
    public static string BuildTooltip()
    {
        var ui = Object.FindFirstObjectByType<SynergyUI>();
        if (ui == null) return "[BuildTooltip] no SynergyUI in scene";
        var canvas = ui.GetComponentInParent<Canvas>();
        if (canvas == null) return "[BuildTooltip] no Canvas";

        var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        Transform existing = canvas.transform.Find("SynergyTooltip");
        SynergyTooltip tip;
        if (existing == null)
        {
            var go = new GameObject("SynergyTooltip", typeof(RectTransform), typeof(Image),
                                    typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(SynergyTooltip));
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvas.transform, false);
            rt.SetAsLastSibling();               // draw on top
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(320f, 100f);

            var bg = go.GetComponent<Image>();
            bg.sprite = rounded; bg.type = Image.Type.Sliced;
            bg.color = new Color(0f, 0f, 0f, 0.88f); bg.raycastTarget = true; // hoverable panel

            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 10, 10); vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;

            var csf = go.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(go.transform, false);
            var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
            nameTmp.text = "Synergy"; nameTmp.fontSize = 22f; nameTmp.fontStyle = FontStyles.Bold;

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGo.transform.SetParent(go.transform, false);
            var bodyTmp = bodyGo.GetComponent<TextMeshProUGUI>();
            bodyTmp.text = "Description"; bodyTmp.fontSize = 16f; bodyTmp.enableWordWrapping = true;

            tip = go.GetComponent<SynergyTooltip>();
            var so = new SerializedObject(tip);
            so.FindProperty("panel").objectReferenceValue    = rt;
            so.FindProperty("nameText").objectReferenceValue = nameTmp;
            so.FindProperty("bodyText").objectReferenceValue = bodyTmp;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else tip = existing.GetComponent<SynergyTooltip>();

        // Wire SynergyUI.tooltip
        var so2 = new SerializedObject(ui);
        so2.FindProperty("tooltip").objectReferenceValue = tip;
        so2.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ui);

        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
        return "[BuildTooltip] tooltip=" + (tip != null) + " wiredToUI";
    }
}
