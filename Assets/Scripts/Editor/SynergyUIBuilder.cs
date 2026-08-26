using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-off editor tooling to build the SynergyBadge prefab and wire the SynergyUI panel.
/// </summary>
public static class SynergyUIBuilder
{
    private const string PrefabPath = "Assets/Resources/Prefabs/UI/SynergyBadge.prefab";

    // Build badge prefab: [ Badge image | (Name / Count) ] in a horizontal row. //
    public static string BuildPrefab()
    {
        Directory.CreateDirectory("Assets/Resources/Prefabs/UI");

        var root = new GameObject("SynergyBadge", typeof(RectTransform), typeof(CanvasGroup),
                                  typeof(HorizontalLayoutGroup), typeof(LayoutElement), typeof(SynergyBadgeUI));
        var rootRt = (RectTransform)root.transform;
        rootRt.sizeDelta = new Vector2(240f, 60f);

        var hlg = root.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(6, 6, 4, 4);
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;  hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        root.GetComponent<LayoutElement>().minHeight = 60f;

        // Badge image (left)
        var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        badge.transform.SetParent(root.transform, false);
        var badgeLe = badge.GetComponent<LayoutElement>();
        badgeLe.preferredWidth = 52f; badgeLe.preferredHeight = 52f;
        badgeLe.minWidth = 52f; badgeLe.minHeight = 52f;
        var badgeImg = badge.GetComponent<Image>();
        badgeImg.preserveAspect = true;

        // Text column (right): Name over Count
        var col = new GameObject("Texts", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        col.transform.SetParent(root.transform, false);
        var vlg = col.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleLeft; vlg.spacing = 0f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        col.GetComponent<LayoutElement>().flexibleWidth = 1f;

        var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(col.transform, false);
        var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
        nameTmp.text = "Name"; nameTmp.fontSize = 20f; nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.enableWordWrapping = false; nameTmp.overflowMode = TextOverflowModes.Ellipsis;

        var countGo = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        countGo.transform.SetParent(col.transform, false);
        var countTmp = countGo.GetComponent<TextMeshProUGUI>();
        countTmp.text = "0/0"; countTmp.fontSize = 15f; countTmp.color = new Color(0.8f, 0.8f, 0.8f, 1f);

        // Wire the component
        var badgeUi = root.GetComponent<SynergyBadgeUI>();
        var so = new SerializedObject(badgeUi);
        so.FindProperty("badge").objectReferenceValue = badgeImg;
        so.FindProperty("nameText").objectReferenceValue = nameTmp;
        so.FindProperty("countText").objectReferenceValue = countTmp;
        so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "[BuildPrefab] saved " + PrefabPath;
    }

    // Wire the scene panel: list container + page button, refs on SynergyUI, hide old text. //
    public static string WireScene()
    {
        var ui = Object.FindFirstObjectByType<SynergyUI>();
        if (ui == null) return "[WireScene] no SynergyUI in scene";
        var panel = (RectTransform)ui.transform;

        // Old text-based display: disable so it no longer shows stale text.
        var oldText = ui.GetComponent<TextMeshProUGUI>();
        if (oldText != null) oldText.enabled = false;

        // List container (top-anchored vertical stack)
        RectTransform container = FindChild(panel, "ListContainer");
        if (container == null)
        {
            var go = new GameObject("ListContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            container = (RectTransform)go.transform;
            container.SetParent(panel, false);
            container.anchorMin = new Vector2(0f, 1f); container.anchorMax = new Vector2(1f, 1f);
            container.pivot = new Vector2(0.5f, 1f);
            container.anchoredPosition = Vector2.zero;
            container.sizeDelta = new Vector2(0f, 0f);
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft; vlg.spacing = 4f;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
            var csf = go.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // Page button
        RectTransform btnRt = FindChild(panel, "PageButton");
        Button btn;
        TextMeshProUGUI pageLabel;
        if (btnRt == null)
        {
            var go = new GameObject("PageButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnRt = (RectTransform)go.transform;
            btnRt.SetParent(panel, false);
            btnRt.anchorMin = new Vector2(0f, 0f); btnRt.anchorMax = new Vector2(1f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.sizeDelta = new Vector2(0f, 36f); btnRt.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
            btn = go.GetComponent<Button>();

            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var lblRt = (RectTransform)lblGo.transform;
            lblRt.SetParent(go.transform, false);
            lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one; lblRt.sizeDelta = Vector2.zero;
            pageLabel = lblGo.GetComponent<TextMeshProUGUI>();
            pageLabel.text = "▼ More"; pageLabel.fontSize = 16f; pageLabel.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            btn = btnRt.GetComponent<Button>();
            pageLabel = btnRt.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        var prefab = AssetDatabase.LoadAssetAtPath<SynergyBadgeUI>(PrefabPath);

        var so = new SerializedObject(ui);
        so.FindProperty("badgePrefab").objectReferenceValue = prefab;
        so.FindProperty("listContainer").objectReferenceValue = container;
        so.FindProperty("pageButton").objectReferenceValue = btn;
        so.FindProperty("pageLabel").objectReferenceValue = pageLabel;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
        return "[WireScene] container=" + (container != null) + " button=" + (btn != null) + " prefab=" + (prefab != null);
    }

    // Add a big unit-count text between the Badge and the text column, wired to largeCountText. //
    public static string AddLargeCount()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform existing = root.transform.Find("LargeCount");
            TextMeshProUGUI tmp;
            if (existing == null)
            {
                var go = new GameObject("LargeCount", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                go.transform.SetParent(root.transform, false);
                go.transform.SetSiblingIndex(1); // Badge(0) | LargeCount(1) | Texts(2)
                var le = go.GetComponent<LayoutElement>();
                le.minWidth = 34f; le.preferredWidth = 42f;
                tmp = go.GetComponent<TextMeshProUGUI>();
                tmp.text = "2"; tmp.fontSize = 34f; tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center; tmp.enableWordWrapping = false;
            }
            else tmp = existing.GetComponent<TextMeshProUGUI>();

            var badgeUi = root.GetComponent<SynergyBadgeUI>();
            var so = new SerializedObject(badgeUi);
            so.FindProperty("largeCountText").objectReferenceValue = tmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return "[AddLargeCount] wired largeCountText (sibling index " + tmp.transform.GetSiblingIndex() + ")";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Wrap the LargeCount text in a gray box with a thin white border. //
    public static string StyleLargeCount()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            if (root.transform.Find("LargeCountBox") != null) return "[StyleLargeCount] already wrapped";
            Transform text = root.transform.Find("LargeCount");
            if (text == null) return "[StyleLargeCount] no LargeCount node";

            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            int slot = text.GetSiblingIndex();

            // White Box
            var box = new GameObject("LargeCountBox", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var boxRt = (RectTransform)box.transform;
            boxRt.SetParent(root.transform, false);
            boxRt.SetSiblingIndex(slot);
            var textLe = text.GetComponent<LayoutElement>();
            var boxLe  = box.GetComponent<LayoutElement>();
            boxLe.minWidth = 40f; boxLe.preferredWidth = textLe != null ? Mathf.Max(44f, textLe.preferredWidth) : 44f;
            boxLe.minHeight = 44f; boxLe.preferredHeight = 44f;
            if (textLe != null) Object.DestroyImmediate(textLe);
            var boxImg = box.GetComponent<Image>();
            boxImg.sprite = rounded; boxImg.type = Image.Type.Sliced;
            boxImg.color = Color.white; boxImg.raycastTarget = false;

            // Gray fill inset by the border thickness (white shows through as the thin border)
            var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
            var bgRt = (RectTransform)bg.transform;
            bgRt.SetParent(box.transform, false);
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(1f, 1f); bgRt.offsetMax = new Vector2(-1f, -1f);
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = rounded; bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.28f, 0.28f, 0.28f, 1f); bgImg.raycastTarget = false;

            // Move the number on top, stretch-fill inside the box
            text.SetParent(box.transform, false);
            var trt = (RectTransform)text;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            text.SetAsLastSibling(); // above BG

            var badgeUi = root.GetComponent<SynergyBadgeUI>();
            var so = new SerializedObject(badgeUi);
            so.FindProperty("largeCountGroup").objectReferenceValue = box;
            so.FindProperty("largeCountText").objectReferenceValue = text.GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return "[StyleLargeCount] wrapped: LargeCountBox(white) > BG(gray) > LargeCount(text)";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Add a semi-transparent panel behind the Name/Count column for readability. //
    public static string AddTextBackground()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform texts = root.transform.Find("Texts");
            if (texts == null) return "[AddTextBackground] no Texts node";

            var img = texts.GetComponent<Image>();
            if (img == null) img = texts.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); // rounded default
            img.type = Image.Type.Sliced;
            img.color = new Color(0f, 0f, 0f, 0.5f); // dark, ~0.5 opacity
            img.raycastTarget = false;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return "[AddTextBackground] panel added on Texts (alpha " + img.color.a + ")";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static RectTransform FindChild(Transform parent, string name)
    {
        foreach (Transform c in parent)
            if (c.name == name) return (RectTransform)c;
        return null;
    }
}
