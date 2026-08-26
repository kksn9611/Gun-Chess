using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-off editor tooling: adds a cost-colored border frame and a high-cost pulsing glow to each
/// shop slot, and wires ShopSlotUI. Layout is a functional first pass — restyle in the editor.
/// </summary>
public static class ShopSlotDecorator
{
    private const string PalettePath = "Assets/Data/ShopCostPalette.asset";
    private const string GlowSprite  = "Assets/Resources/Images/Textures/bubble_soft01.png";
    private static readonly Color InnerColor = new Color(0.16f, 0.17f, 0.22f, 0.95f); // matches slot bg

    public static string Build()
    {
        ShopCostPalette palette = EnsurePalette();
        Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite soft    = AssetDatabase.LoadAssetAtPath<Sprite>(GlowSprite);

        var slots = Object.FindObjectsByType<ShopSlotUI>(FindObjectsSortMode.None);
        if (slots.Length == 0) return "[ShopSlotDecorator] no ShopSlotUI in scene";

        int done = 0;
        foreach (var slot in slots)
        {
            var slotRt = (RectTransform)slot.transform;

            // Glow (backmost): soft cost-tinted sprite, larger than the slot, pulsing. Hidden by default.
            var glow = FindOrCreateImage(slotRt, "Glow");
            glow.sprite = soft; glow.type = Image.Type.Simple; glow.raycastTarget = false;
            glow.color = new Color(1f, 1f, 1f, 0.7f);
            StretchFill((RectTransform)glow.transform, -14f); // overspill = halo
            glow.transform.SetSiblingIndex(0);
            if (glow.GetComponent<GlowPulse>() == null) glow.gameObject.AddComponent<GlowPulse>();
            glow.gameObject.SetActive(false);

            // CostFrame (over glow, under content): outer color ring + inner dark inset.
            var frame = FindOrCreateImage(slotRt, "CostFrame");
            frame.sprite = rounded; frame.type = Image.Type.Sliced; frame.raycastTarget = false;
            frame.color = Color.white;
            StretchFill((RectTransform)frame.transform, 0f);
            frame.transform.SetSiblingIndex(1);

            var inner = FindOrCreateImage((RectTransform)frame.transform, "Inner");
            inner.sprite = rounded; inner.type = Image.Type.Sliced; inner.raycastTarget = false;
            inner.color = InnerColor;
            StretchFill((RectTransform)inner.transform, 3f); // 3px ring = border thickness

            // Wire ShopSlotUI
            var so = new SerializedObject(slot);
            so.FindProperty("palette").objectReferenceValue   = palette;
            so.FindProperty("costFrame").objectReferenceValue = frame;
            so.FindProperty("glow").objectReferenceValue      = glow;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(slot);
            done++;
        }

        var scene = slots[0].gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"[ShopSlotDecorator] decorated {done} slots, palette={(palette != null)}, glowSprite={(soft != null)}";
    }

    private static ShopCostPalette EnsurePalette()
    {
        var pal = AssetDatabase.LoadAssetAtPath<ShopCostPalette>(PalettePath);
        if (pal == null)
        {
            Directory.CreateDirectory("Assets/Data");
            pal = ScriptableObject.CreateInstance<ShopCostPalette>();
            AssetDatabase.CreateAsset(pal, PalettePath);
            AssetDatabase.SaveAssets();
        }
        return pal;
    }

    private static Image FindOrCreateImage(RectTransform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>();
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    private static void StretchFill(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        rt.localScale = Vector3.one;
    }
}
