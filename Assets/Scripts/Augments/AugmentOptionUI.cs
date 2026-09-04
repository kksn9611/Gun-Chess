using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>One augment choice card. Bind an AugmentData + a pick callback.</summary>
public class AugmentOptionUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [Tooltip("Card frame/background tinted by rarity (defaults to this object's Image)")]
    [SerializeField] private Image background;

    private AugmentData data;
    private Action<AugmentData> onPick;

    private void Awake()
    {
        if (background == null) background = GetComponent<Image>(); // fallback: tint the root image
        if (button != null) button.onClick.AddListener(OnClick);
    }

    private void OnClick() => onPick?.Invoke(data);

    /// <summary>Bind a choice (or null to hide this card).</summary>
    public void Bind(AugmentData a, Action<AugmentData> pick)
    {
        data = a;
        onPick = pick;
        if (nameText != null) nameText.text = a != null ? a.augmentName : string.Empty;
        if (descText != null) descText.text = a != null ? a.description : string.Empty;
        if (background != null && a != null) background.color = a.rarity.ToColor(); // distinguish by rarity
        gameObject.SetActive(a != null);
    }
}
