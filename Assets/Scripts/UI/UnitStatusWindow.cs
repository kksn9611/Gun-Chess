using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Prototype unit status window: portrait, name, cost/star, traits, core stats, and skill.
/// Bind a live UnitController (current stats) or a UnitData (base preview).
/// </summary>
public class UnitStatusWindow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panel;           // root shown/hidden (defaults to this)
    [SerializeField] private Image portrait;                // unit portrait (shop-slot card UnitImage)
    [SerializeField] private Image costFrame;               // card border, tinted by cost (shop style)
    [SerializeField] private ShopCostPalette palette;       // shared cost -> color table (same as shop)
    [SerializeField] private TextMeshProUGUI nameText;      // unit name
    [SerializeField] private TextMeshProUGUI priceText;     // gold cost with coin sprite
    [SerializeField] private TextMeshProUGUI metaText;      // star + traits
    [SerializeField] private Image hpFill;                  // HP bar fill (Filled Horizontal)
    [SerializeField] private TextMeshProUGUI hpText;        // "curHP / maxHP" overlay
    [SerializeField] private Image mpFill;                  // MP bar fill (Filled Horizontal)
    [SerializeField] private TextMeshProUGUI mpText;        // "curMP / maxMP" overlay
    [SerializeField] private SynergyRowUI[] traitRows;      // shop-style trait rows (icon + name)
    [SerializeField] private TextMeshProUGUI statsText;     // core combat stats
    [SerializeField] private TextMeshProUGUI skillNameText; // skill name
    [SerializeField] private TextMeshProUGUI skillDescText; // skill description

    [Header("Live Refresh")]
    [SerializeField] private float refreshInterval = 0.5f; // stats poll rate while a live unit is shown

    private readonly StringBuilder sb = new StringBuilder();

    private UnitStats boundStats; // live unit currently shown; bars follow its HP/MP events

    private void Awake()
    {
        if (panel == null) panel = (RectTransform)transform;
        Hide();
    }

    private void OnDisable() => Unbind(); // drop event refs if the window is disabled/destroyed

    // Show / Hide //

    /// <summary>Show live stats for a placed unit; bars update in real time until hidden or replaced.</summary>
    public void Show(UnitController unit)
    {
        if (unit == null || unit.Stats == null || unit.Stats.UnitData == null) { Hide(); return; }
        UnitStats s = unit.Stats;

        Bind(s); // follow HP/MP changes live
        SetHeader(s.UnitData, s.StarLevel);
        SetTraits(s.UnitData);
        SetBars(s.CurrentHp, s.CurrentMaxHp, s.CurrentMp, s.CurrentMaxMp);
        if (statsText != null) statsText.text = BuildLiveStats(s);
        SetSkill(s.UnitData.skill);

        panel.gameObject.SetActive(true);
    }

    /// <summary>Show base stats for a unit definition (e.g. shop preview).</summary>
    public void Show(UnitData data)
    {
        if (data == null) { Hide(); return; }

        Unbind(); // static preview: no live unit to track
        SetHeader(data, data.starLevel);
        SetTraits(data);
        SetBars(data.maxHp, data.maxHp, 0f, data.maxMp); // base preview: full HP, empty MP
        if (statsText != null) statsText.text = BuildBaseStats(data);
        SetSkill(data.skill);

        panel.gameObject.SetActive(true);
    }

    // Live Binding //

    private void Bind(UnitStats stats)
    {
        Unbind();
        boundStats = stats;
        boundStats.OnHpChanged += OnHpChanged;
        boundStats.OnMpChanged += OnMpChanged;
        // Poll the text stats (Attack, Atk Speed, ...) since they have no per-change events.
        if (refreshInterval > 0f) InvokeRepeating(nameof(RefreshStats), refreshInterval, refreshInterval);
    }

    private void Unbind()
    {
        if (boundStats != null)
        {
            boundStats.OnHpChanged -= OnHpChanged;
            boundStats.OnMpChanged -= OnMpChanged;
        }
        CancelInvoke(nameof(RefreshStats));
        boundStats = null;
    }

    private void OnHpChanged(float cur, float max) => SetBar(hpFill, hpText, cur, max);
    private void OnMpChanged(float cur, float max) => SetBar(mpFill, mpText, cur, max);

    /// <summary>Re-read the shown unit's live stats (buffs/debuffs change these mid-combat).</summary>
    private void RefreshStats()
    {
        if (boundStats == null) return;
        if (statsText != null) statsText.text = BuildLiveStats(boundStats);
    }

    public void Hide()
    {
        Unbind();
        if (panel != null) panel.gameObject.SetActive(false);
    }

    // Binding //

    private void SetHeader(UnitData data, int star)
    {
        if (portrait != null)
        {
            portrait.sprite  = data.portrait;
            portrait.enabled = data.portrait != null; // hide when unassigned
        }
        if (costFrame != null && palette != null) costFrame.color = palette.ColorFor(data.cost); // shop-style cost border
        if (nameText  != null) nameText.text  = data.unitName;
        if (priceText != null) priceText.text = $"<sprite=0>{data.cost}"; // gold price, like the shop
        if (metaText  != null) metaText.text  = BuildMeta(data, star);
    }

    private void SetBars(float hp, float maxHp, float mp, float maxMp)
    {
        SetBar(hpFill, hpText, hp, maxHp);
        SetBar(mpFill, mpText, mp, maxMp);
    }

    private static void SetBar(Image fill, TextMeshProUGUI text, float cur, float max)
    {
        if (fill != null) fill.fillAmount = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
        if (text != null) text.text = $"{Mathf.RoundToInt(cur)} / {Mathf.RoundToInt(max)}";
    }

    private void SetSkill(BaseSkill skill)
    {
        if (skillNameText != null) skillNameText.text = skill != null ? skill.skillName : "-";
        if (skillDescText != null) skillDescText.text = skill != null ? skill.description : string.Empty;
    }

    // Star level as star sprites (<sprite=1>), one per level. Traits are shop-style rows (SetTraits).
    private string BuildMeta(UnitData data, int star)
    {
        sb.Clear();
        int n = Mathf.Max(1, star);
        for (int i = 0; i < n; i++) sb.Append("<sprite=1>");
        return sb.ToString();
    }

    /// <summary>Fill the shop-style trait rows from the unit's synergies; hide the extras.</summary>
    private void SetTraits(UnitData data)
    {
        if (traitRows == null) return;
        SynergyData[] syn = data.synergies;
        int synCount = syn != null ? syn.Length : 0;

        // Fill each valid row with the next synergy; a missing row doesn't consume one.
        int s = 0;
        for (int i = 0; i < traitRows.Length; i++)
        {
            if (traitRows[i] == null) continue;
            traitRows[i].Set(s < synCount ? syn[s++] : null); // Set(null) hides the row
        }
    }

    // Live (post-buff) stats from a placed unit.
    private string BuildLiveStats(UnitStats s)
    {
        sb.Clear(); // HP/MP shown as bars, not here
        Line("공격력   ",    Mathf.RoundToInt(s.CurrentAtt).ToString());
        Line("방어력   ",   Mathf.RoundToInt(s.CurrentDef).ToString());
        Line("공격속도", s.CurrentAttSpd.ToString("0.00"));
        Line("사거리   ",     Mathf.RoundToInt(s.CurrentAttRange).ToString());
        Line("치명타율", $"{Mathf.RoundToInt(s.CurrentCritChance * 100f)}%  x{s.CurrentCritDamage:0.0}");
        Line("스킬배율", $"{Mathf.RoundToInt(s.SkillDamageMultiplier * 100f)}%");
        Line("생명흡수", $"{Mathf.RoundToInt(s.CurrentLifesteal * 100f)}%");
        return sb.ToString();
    }

    // Base stats from the unit definition.
    private string BuildBaseStats(UnitData d)
    {
        sb.Clear(); // HP/MP shown as bars, not here
        Line("공격력   ",    Mathf.RoundToInt(d.att).ToString());
        Line("방어력   ",   Mathf.RoundToInt(d.def).ToString());
        Line("공격속도", d.attSpd.ToString("0.00"));
        Line("사거리   ",     Mathf.RoundToInt(d.attRange).ToString());
        Line("치명타율",      $"{Mathf.RoundToInt(d.critChance * 100f)}%  x{d.critDamage:0.0}");
        Line("스킬배율", "100%"); // UnitData has no base skill-dmg field; default multiplier = 1
        Line("생명흡수", "0%");   // UnitData has no base lifesteal field; default = 0
        return sb.ToString();
    }

    private void Line(string label, string value)
    {
        if (sb.Length > 0) sb.Append('\n');
        sb.Append("<b>").Append(label).Append("</b>  ").Append(value);
    }
}
