/// <summary>
/// Stable identifiers for catalog-driven sounds (UI, BGM, shared SFX). Explicit values so entries
/// stay bound if the list is reordered. Per-unit attack/skill clips are NOT here — they stay on the unit.
/// </summary>
public enum SoundId
{
    None = 0,

    // UI (10-49)
    UiClick = 10,
    UiHover = 11,
    UiPurchase = 12,
    UiSell = 13,
    UiReroll = 14,
    UiError = 15,
    UiLevelUp = 16,
    UiTransition = 17,
    UiSelect = 18, // augment pick confirm

    // BGM (50-99)
    MainBgm = 50,
    TitleBgm = 51,

    // Result stingers — played as SFX one-shots (values kept for catalog binding)
    Victory = 52,
    Defeat = 53,

    // Shared SFX (100+)
    UnitLevelUpStar2 = 100,
    UnitLevelUpStar3 = 101,
}
