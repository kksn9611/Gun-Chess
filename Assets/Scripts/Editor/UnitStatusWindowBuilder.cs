using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-off editor tooling: builds the prototype UnitStatusWindow prefab and wires its fields.
/// </summary>
public static class UnitStatusWindowBuilder
{
    private const string PrefabPath = "Assets/Resources/Prefabs/UI/UnitStatusWindow.prefab";

    // Shared shop-style assets (reused so the header matches the ShopPanel exactly).
    private const string FontPath    = "Assets/TextMesh Pro/Fonts/PretendardVariable SDF.asset";
    private const string CoinPath    = "Assets/Resources/Sprite Assets/coin01.asset";
    private const string PalettePath = "Assets/Data/ShopCostPalette.asset";
    private static readonly Color CardBg = new Color32(0x29, 0x2B, 0x38, 0xF2); // shop slot fill
    private static readonly Color Gold   = new Color32(0xFF, 0xD6, 0x00, 0xFF); // shop price gold

    [MenuItem("Tools/UI/Build Unit Status Window")]
    public static void BuildMenu() => Debug.Log(Build());

    [MenuItem("Tools/UI/Restyle Status Window Header (Shop Card)")]
    public static void RestyleHeaderMenu() => Debug.Log(RestyleHeader());

    [MenuItem("Tools/UI/Add Status Window HP/MP Bars")]
    public static void AddBarsMenu() => Debug.Log(AddBars());

    [MenuItem("Tools/UI/Add Status Window Traits (Shop Style)")]
    public static void AddTraitsMenu() => Debug.Log(AddTraits());

    // Shop-style trait rows: SynergyList (VLG) of SynergyRow (icon 36 + name 25 #E6E6F2). //
    public static string AddTraits()
    {
        const int rowCount = 3; // fixed pool; extras collapse
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var oldList = root.transform.Find("Traits");
            if (oldList != null) Object.DestroyImmediate(oldList.gameObject);

            // SynergyList clone.
            var listGo = new GameObject("Traits", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listGo.transform.SetParent(root.transform, false);
            var vlg = listGo.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 0); vlg.spacing = 4f;
            vlg.childControlWidth = true;  vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.LowerLeft;
            var csf = listGo.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var rows = new SynergyRowUI[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                var rowGo = new GameObject("SynergyRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement), typeof(SynergyRowUI));
                rowGo.transform.SetParent(listGo.transform, false);
                var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(0, 0, 0, 0); hlg.spacing = 4f;
                hlg.childControlWidth = true;  hlg.childForceExpandWidth = false;
                hlg.childControlHeight = true; hlg.childForceExpandHeight = false;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                var rowLe = rowGo.GetComponent<LayoutElement>();
                rowLe.minHeight = 30f; rowLe.preferredHeight = 30f;

                var icon = MakeImage("SynergyIcon", rowGo.transform, null);
                icon.color = Color.white;
                var iconLe = icon.gameObject.AddComponent<LayoutElement>();
                iconLe.minWidth = 36f; iconLe.minHeight = 36f; iconLe.preferredWidth = 36f; iconLe.preferredHeight = 36f;

                var label = MakeText("SynergyName", rowGo.transform, "Trait", 25f, FontStyles.Normal, new Color32(0xE6, 0xE6, 0xF2, 0xFF), TextAlignmentOptions.MidlineLeft, font);
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

                var row = rowGo.GetComponent<SynergyRowUI>();
                var rso = new SerializedObject(row);
                rso.FindProperty("icon").objectReferenceValue  = icon;
                rso.FindProperty("label").objectReferenceValue = label;
                rso.ApplyModifiedPropertiesWithoutUndo();
                rows[i] = row;
            }

            // Place right after Meta.
            var meta = root.transform.Find("Meta");
            listGo.transform.SetSiblingIndex(meta != null ? meta.GetSiblingIndex() + 1 : 1);

            // Wire the traitRows array.
            var win = root.GetComponent<UnitStatusWindow>();
            var so = new SerializedObject(win);
            var arr = so.FindProperty("traitRows");
            arr.arraySize = rowCount;
            for (int i = 0; i < rowCount; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return $"[AddTraits] rows={rowCount} font={(font != null)} saved {PrefabPath}";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Replace the HP/MP stat lines with filled bars + centered "cur / max" overlay. //
    public static string AddBars()
    {
        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var font     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            // Remove any previous bars (idempotent re-run).
            foreach (var n in new[] { "HPBar", "MPBar" })
            {
                var old = root.transform.Find(n);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            var hp = BuildBar(root.transform, "HPBar", uiSprite, font, new Color32(0x3F, 0xC3, 0x55, 0xFF), out Image hpFill, out TextMeshProUGUI hpText);
            var mp = BuildBar(root.transform, "MPBar", uiSprite, font, new Color32(0x2E, 0x9B, 0xF0, 0xFF), out Image mpFill, out TextMeshProUGUI mpText);

            // Order: Header, Meta, HPBar, MPBar, then the rest.
            var meta = root.transform.Find("Meta");
            int at = meta != null ? meta.GetSiblingIndex() + 1 : 1;
            hp.transform.SetSiblingIndex(at);
            mp.transform.SetSiblingIndex(at + 1);

            var win = root.GetComponent<UnitStatusWindow>();
            var so = new SerializedObject(win);
            so.FindProperty("hpFill").objectReferenceValue = hpFill;
            so.FindProperty("hpText").objectReferenceValue = hpText;
            so.FindProperty("mpFill").objectReferenceValue = mpFill;
            so.FindProperty("mpText").objectReferenceValue = mpText;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return $"[AddBars] font={(font!=null)} saved {PrefabPath}";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // One bar: dark track (LayoutElement height) + Filled fill + centered white "cur / max". //
    private static GameObject BuildBar(Transform parent, string name, Sprite uiSprite, TMP_FontAsset font, Color fillColor,
        out Image fill, out TextMeshProUGUI label)
    {
        var bar = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        bar.transform.SetParent(parent, false);
        var track = bar.GetComponent<Image>();
        track.sprite = uiSprite; track.type = Image.Type.Sliced; track.color = new Color32(0x15, 0x17, 0x1F, 0xFF); track.raycastTarget = false;
        var le = bar.GetComponent<LayoutElement>();
        le.minHeight = 22f; le.preferredHeight = 22f; le.flexibleHeight = 0f;

        fill = MakeImage("Fill", bar.transform, uiSprite);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.color = fillColor; fill.fillAmount = 1f;
        Stretch((RectTransform)fill.transform, new Vector2(2f, 2f), new Vector2(-2f, -2f));

        label = MakeText("Label", bar.transform, "0 / 0", 13f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, font);
        Stretch((RectTransform)label.transform, Vector2.zero, Vector2.zero);

        return bar;
    }

    // Rebuild the header as a shop-slot card (cost-framed portrait + name + gold price). //
    public static string RestyleHeader()
    {
        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var font     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var coin     = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(CoinPath);
        var palette  = AssetDatabase.LoadAssetAtPath<ShopCostPalette>(PalettePath);

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(248f, 100f); // ~ shop slot width + padding

            // Drop the old header (portrait/name/meta), keep stats & skill blocks.
            var oldHeader = root.transform.Find("Header");
            if (oldHeader != null) Object.DestroyImmediate(oldHeader.gameObject);

            // Card root = cost-tinted border (Header's own Image), fixed shop-slot height.
            var header = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            header.transform.SetParent(root.transform, false);
            var costFrame = header.GetComponent<Image>();
            costFrame.sprite = uiSprite; costFrame.type = Image.Type.Sliced; costFrame.color = Color.white;
            var hLe = header.GetComponent<LayoutElement>();
            hLe.minHeight = 170f; hLe.preferredHeight = 170f; hLe.flexibleHeight = 0f;

            // Inner dark fill (inset 3px, like the shop CostFrame->Inner).
            var inner = MakeImage("Inner", header.transform, uiSprite);
            inner.color = CardBg;
            Stretch((RectTransform)inner.transform, new Vector2(3f, 3f), new Vector2(-3f, -3f));

            // Portrait fills the top region.
            var portrait = MakeImage("UnitImage", header.transform, null);
            portrait.color = new Color32(0x99, 0x99, 0x99, 0xFF); // placeholder look
            var pRt = (RectTransform)portrait.transform;
            pRt.anchorMin = new Vector2(0f, 1f); pRt.anchorMax = new Vector2(1f, 1f); pRt.pivot = new Vector2(0.5f, 1f);
            pRt.offsetMin = new Vector2(4f, -140f); pRt.offsetMax = new Vector2(-4f, -4f);

            // Name (bottom-left) + Price (bottom-right, gold + coin) — shop text styles.
            var nameText = MakeText("Name", header.transform, "Unit Name", 18f, FontStyles.Normal, Color.white, TextAlignmentOptions.MidlineLeft, font);
            var nRt = (RectTransform)nameText.transform;
            nRt.anchorMin = new Vector2(0f, 0f); nRt.anchorMax = new Vector2(1f, 0f); nRt.pivot = new Vector2(0.5f, 0.5f);
            nRt.offsetMin = new Vector2(6f, 4f); nRt.offsetMax = new Vector2(-58f, 28f);

            var priceText = MakeText("Price", header.transform, "<sprite=0>1", 16f, FontStyles.Normal, Gold, TextAlignmentOptions.MidlineRight, font);
            priceText.spriteAsset = coin;
            var prRt = (RectTransform)priceText.transform;
            prRt.anchorMin = new Vector2(1f, 0f); prRt.anchorMax = new Vector2(1f, 0f); prRt.pivot = new Vector2(1f, 0f);
            prRt.offsetMin = new Vector2(-56f, 4f); prRt.offsetMax = new Vector2(-6f, 28f);

            // Traits/star line under the card.
            var metaText = MakeText("Meta", root.transform, "Star 1   Traits", 14f, FontStyles.Normal, new Color(0.7f, 0.75f, 0.85f, 1f), TextAlignmentOptions.TopLeft, font);

            // Match the shop font on the existing stat/skill texts too.
            var stats     = FindText(root.transform, "Stats");
            var skillName = FindText(root.transform, "SkillName");
            var skillDesc = FindText(root.transform, "SkillDesc");
            foreach (var t in new[] { stats, skillName, skillDesc }) if (t != null && font != null) t.font = font;

            // Order: Header, Meta, Stats, SkillName, SkillDesc.
            header.transform.SetSiblingIndex(0);
            metaText.transform.SetSiblingIndex(1);

            // Rewire component fields.
            var win = root.GetComponent<UnitStatusWindow>();
            var so = new SerializedObject(win);
            so.FindProperty("panel").objectReferenceValue         = rootRt;
            so.FindProperty("portrait").objectReferenceValue      = portrait;
            so.FindProperty("costFrame").objectReferenceValue     = costFrame;
            so.FindProperty("palette").objectReferenceValue       = palette;
            so.FindProperty("nameText").objectReferenceValue      = nameText;
            so.FindProperty("priceText").objectReferenceValue     = priceText;
            so.FindProperty("metaText").objectReferenceValue      = metaText;
            so.FindProperty("statsText").objectReferenceValue     = stats;
            so.FindProperty("skillNameText").objectReferenceValue = skillName;
            so.FindProperty("skillDescText").objectReferenceValue = skillDesc;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return $"[RestyleHeader] font={(font!=null)} coin={(coin!=null)} palette={(palette!=null)} saved {PrefabPath}";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static TextMeshProUGUI FindText(Transform root, string name)
    {
        var t = root.Find(name);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    private static void Stretch(RectTransform rt, Vector2 offMin, Vector2 offMax)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = offMin; rt.offsetMax = offMax;
    }

    // Build the panel: header (portrait + name/meta), stats block, skill block. //
    public static string Build()
    {
        var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Root: vertical stack, auto-height, dark rounded panel.
        var root = new GameObject("UnitStatusWindow",
            typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(UnitStatusWindow));
        var rootRt = (RectTransform)root.transform;
        rootRt.sizeDelta = new Vector2(300f, 100f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);

        var bg = root.GetComponent<Image>();
        bg.sprite = rounded; bg.type = Image.Type.Sliced;
        bg.color = new Color(0.06f, 0.07f, 0.10f, 0.95f);

        var vlg = root.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 12, 12); vlg.spacing = 8f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;  vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true; vlg.childForceExpandHeight = false;

        var csf = root.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // Header row: portrait + (name over meta).
        var header = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        header.transform.SetParent(root.transform, false);
        var hlg = header.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f; hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;  hlg.childForceExpandWidth = false;
        hlg.childControlHeight = true; hlg.childForceExpandHeight = false;
        header.GetComponent<LayoutElement>().minHeight = 64f;

        var portrait = MakeImage("Portrait", header.transform, rounded);
        portrait.color = new Color(0.20f, 0.20f, 0.24f, 1f); // placeholder look
        var portLe = portrait.gameObject.AddComponent<LayoutElement>();
        portLe.preferredWidth = 64f; portLe.preferredHeight = 64f;
        portLe.minWidth = 64f; portLe.minHeight = 64f;

        var headerText = new GameObject("HeaderText", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        headerText.transform.SetParent(header.transform, false);
        var htVlg = headerText.GetComponent<VerticalLayoutGroup>();
        htVlg.spacing = 2f; htVlg.childAlignment = TextAnchor.MiddleLeft;
        htVlg.childControlWidth = true; htVlg.childForceExpandWidth = true;
        htVlg.childControlHeight = true; htVlg.childForceExpandHeight = false;
        headerText.GetComponent<LayoutElement>().flexibleWidth = 1f;

        var nameText = MakeText("Name", headerText.transform, "Unit Name", 22f, FontStyles.Bold, Color.white);
        var metaText = MakeText("Meta", headerText.transform, "Star 1   1g   Traits", 14f, FontStyles.Normal, new Color(0.7f, 0.75f, 0.85f, 1f));

        // Stats block (multi-line).
        var statsText = MakeText("Stats", root.transform,
            "<b>HP</b>  0 / 0\n<b>MP</b>  0 / 0\n<b>Attack</b>  0\n<b>Defense</b>  0\n<b>Atk Speed</b>  0.00\n<b>Range</b>  0\n<b>Crit</b>  0%  x0.0",
            16f, FontStyles.Normal, new Color(0.9f, 0.9f, 0.9f, 1f));

        // Skill block.
        var skillName = MakeText("SkillName", root.transform, "Skill Name", 18f, FontStyles.Bold, new Color(1f, 0.85f, 0.4f, 1f));
        var skillDesc = MakeText("SkillDesc", root.transform, "Skill description goes here.", 14f, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f, 1f));
        skillDesc.enableWordWrapping = true;

        // Wire the component fields.
        var win = root.GetComponent<UnitStatusWindow>();
        var so = new SerializedObject(win);
        so.FindProperty("panel").objectReferenceValue         = rootRt;
        so.FindProperty("portrait").objectReferenceValue      = portrait;
        so.FindProperty("nameText").objectReferenceValue      = nameText;
        so.FindProperty("metaText").objectReferenceValue      = metaText;
        so.FindProperty("statsText").objectReferenceValue     = statsText;
        so.FindProperty("skillNameText").objectReferenceValue = skillName;
        so.FindProperty("skillDescText").objectReferenceValue = skillDesc;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "[UnitStatusWindowBuilder] saved " + PrefabPath;
    }

    // Helpers //

    private static Image MakeImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite; img.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple; img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, FontStyles style, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft, TMP_FontAsset font = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style; tmp.color = color;
        tmp.alignment = alignment;
        if (font != null) tmp.font = font;
        tmp.raycastTarget = false;
        return tmp;
    }
}
