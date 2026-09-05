# Gun-Chess — Junior Developer Onboarding Guide

Welcome to Gun-Chess. This guide gets you from a fresh clone to shipping content in the project. It is written
against the code as of September 2026 (commit `3b70fda`, branch `main`). When the guide and the code disagree,
trust the code and fix the guide.

Read it in order the first time. After that, Part 3 is the section you will keep coming back to.

---

## Part 1 — Project Overview

### 1.1 What Gun-Chess is

Gun-Chess is a **TFT-style autobattler** (Teamfight Tactics / Auto Chess genre) built in **Unity 6000.3.10f1**
with **URP** and **C#**. The player buys gunslinger units from a shop, places them on a hex board, and watches
them fight AI-controlled enemy waves. Between fights they earn gold, level up, merge duplicate units into
stronger stars, and pick powerful run-wide augments.

The whole design is **data-driven**: units, stages, skills, synergies, augments, sounds, and shop odds all live
in ScriptableObject assets under `Assets/Data`. Most content work never touches C#.

### 1.2 Current status

The project is a **playable single-player prototype** with a complete run from Title to Game Clear:

| Area | State |
|---|---|
| Stages | 25 hand-authored stages (`Assets/Data/Stages/Stage1..25`) with a gradual difficulty ramp; a Game Clear screen after stage 25 |
| Champions | 38 base champions across 6 factions (Chaos, Divinity, Elite, Heretic, Innovation, Mystic), each with Star 1/2/3 data + prefabs |
| Synergies | 7 class synergies (the six factions + Enforcer) and 6 weapon synergies (Assault Rifle, MG, Pistol, SMG, Shotgun, Sniper) |
| Skills | 12 skill types, 52 skill assets, per-unit VFX in `Resources/Prefabs/Effect/Unit_Skills` |
| Augments | 4 rarities (Common / Bronze / Silver / Gold); stat, synergy-count, and gold augments; weighted pool; offered on rounds 1, 3, 5 |
| Economy | Gold, interest, EXP/levels, board capacity by level, shop with reroll / lock / pool |
| Combat | Hex A* pathfinding, FSM unit AI, crit / lifesteal / shields / stuns / debuffs, anti-stall escalation |
| Presentation | Title scene with persistent skybox hand-off, Main scene with intro animations, HP/MP bars, tooltips, sound manager with BGM crossfade |

Player-facing strings on data assets (unit names, synergy names, augment descriptions) are **Korean**.
Everything in code, comments, logs, and docs is **English only**. This is a hard project rule.

### 1.3 The core gameplay loop

One round looks like this:

```
Preparation ──(Space)──> Pre-battle bar ──> Battle ──> Result (5 s) ──> Cleanup + income ──> next Preparation
```

1. **Preparation** — the shop shows 5 units. The player buys, sells, rerolls, drags units between bench and
   board, and (on augment rounds) picks 1 of 3 augments. Next round's enemies are already previewed on the
   far side of the board.
2. **Pre-battle transition** — pressing Space (or the Start button) plays a 2 s drain bar. The board is
   captured at the *end* of the bar, so last-second moves count.
3. **Battle** — `BattleManager.StartBattle()` fires `OnBattleStart`. Every fielded unit's AI wakes with a
   random 0–300 ms stagger and runs its FSM until one team is wiped.
4. **Result** — 5 s for death animations.
5. **Cleanup + income** — enemies destroyed, tiles cleared, player units restored to their saved tiles,
   VFX pools trimmed, then income in this exact order: **interest → base turn gold → synergy gold → EXP**.
   The shop refreshes (unless locked), free rerolls are granted, and the round counter advances.
6. After the last stage, `GameClearUI` dims the screen with Return-to-Title / Quit.

Key numbers (all tunable in the Inspector on `PlayerManager` unless noted):

| Value | Default |
|---|---|
| Starting gold / level | 3 / 1 |
| Base turn gold | 2 |
| Interest | 1 gold per 10 held, counting up to 50 gold (max 5) |
| EXP per round | 2 |
| EXP to level (1→2 … 9→10) | 2, 2, 6, 10, 20, 36, 48, 76, 84 (`PlayerManager.expTable`, code) |
| Board capacity | equals player level (1..10) + synergy bonus (`BoardManager`, code) |
| Reroll cost | 2 (`ShopManager`) |
| Merge | 3 copies → next star; 1★ = 1 copy, 2★ = 3, 3★ = 9 |
| Result phase | 5 s (`RoundManager`, code) |

### 1.4 Tech stack

| Dependency | Why we use it |
|---|---|
| **UniTask** (`Cysharp.Threading.Tasks`) | All async gameplay code: skills, AI, round flow. We use `CancellationToken` everywhere; coroutines are legacy. |
| **DOTween** | UI and camera tweens, skybox rotation easing, UI effect loops |
| **UIEffect** (`Coffee.UIEffects`) | Shiny transitions on augment cards |
| **Unity Input System** | Space-to-start, mouse drag placement |
| **URP 17.3** | Rendering |
| **unity-cli** | Command-line control of the editor (compile, play, exec C#). Required workflow tool; see Part 3. |

### 1.5 Repository layout

```
Assets/
  Scenes/            Title.unity, Main.unity
  Scripts/
    Managers/        Battle, Unit, Player, Shop, UnitPool, Merge, Board, Tile, Bench, VFX pools
    Unit/            UnitController + UnitStats/AI/Animator/Movement/Visuals/CCHandler, Spawner, Placer
    Round/           RoundManager, StageData
    Skills/          BaseSkill + 12 skill types, Projectile, SkillArea
    Synergies/       SynergyData/State/Manager + 9 behaviors
    Augments/        AugmentData/Effect/Pool/Manager + UI
    Shop/            ProbabilityTable
    Combat/          Anti-stall system, pre-battle bar
    TileScript/      BaseTile, Hex/ (TileScript, HexGridLayout), Bench/
    Utility/         HexCoordCal, Pathfinder, priority queue, area helpers
    UI/              Bars, shop UI, synergy panel, tooltips, status window, game clear
    Audio/           SoundManager, SoundLibrary, SoundId
    Environment/     PersistentBackground, MapIntro
    Title/           TitleMenu
    Editor/          Batch tools (StarUpGenerator, UI builders, custom inspectors)
    Enums/           StatType, Team, UnitState
  Data/
    Units/           UnitData.cs, UnitPoolDatabase.cs, {Faction}/Star1|2|3/*.asset, Enemy/
    Skills/          {Faction}/*.asset
    Synergies/       {Synergy}/{name}.asset + Tier/, Weapons/{Weapon}/, SynergyState.asset
    Augments/        StatAugments/, SynergyAugments/, GoldAugments/ (by rarity), DefaultAugmentPool.asset
    Stages/          Stage1..25.asset
    Shop/            ShopProbabilityTable.asset
  Resources/
    Prefabs/Units/{Faction}_Units/Star1|2|3/   unit prefabs
    Prefabs/Effect/Unit_Skills/                skill VFX
    Prefabs/UI/                                UI prefabs (AugmentSelectPanel, MenuButton, ...)
    Sound/{BGM,Gun,Skill,UI}/                  clips
CLAUDE.md            project rules (read it)
unity-cli/ChangeLog.md   session-by-session change log (append to it)
```

---

## Part 2 — Core Architecture

### 2.1 Scenes and persistence

Two scenes: **Title** and **Main**. Title pre-loads Main in the background (`TitleMenu.StartGame`) while the
skybox accelerates, then activates it. Two objects survive the swap via `DontDestroyOnLoad`:

- `PersistentBackground` — the skybox. Owns the rotation hand-off (accelerate on Title, flip + decelerate on
  Main) and rebuilds its material instance when you return to Title.
- `SoundManager` — pooled audio voices and the BGM crossfade pair.

Everything else is scene-local and rebuilt on load.

### 2.2 The manager pattern (two flavors)

**MonoBehaviour singletons** — need a scene object; expose `static Instance`; `Awake` sets it or destroys
the duplicate:

| Manager | Owns |
|---|---|
| `BattleManager` | Phase (`Preparation / Battle / Result`), static events `OnBattleStart`, `OnBattleEnd(Team)`, `OnPreparationStart` |
| `UnitManager` | Per-team rosters (`playerUnits`, `enemyUnits`), `CheckBattleEnd()` |
| `PlayerManager` | Gold, EXP, level, interest; static `OnGoldChanged/OnExpChanged/OnLevelChanged` |
| `UnitPool` | Shared TFT-style unit pool (copies per base champion), weighted roll |
| `MergeManager` | 3-copy merges, cascades, merge VFX hand-off |
| `BoardManager` | Field capacity = level + synergy bonus + modifiers; auto-benches overflow |
| `SynergyManager` | Tallies synergies, writes `SynergyState`, re-applies synergy + augment buffs |
| `AugmentManager` | Owned augments (source of truth), aggregates their effects |
| `SoundManager` | Audio service (persistent) |
| `RoundManager`, `ShopManager` | Not singletons — referenced by serialized fields |

**Pure C# singletons** — no scene object, `Instance => instance ??= new ...()`:

| Class | Owns |
|---|---|
| `TileManager` | `Vector2Int → TileScript` map, `ClearAllOccupied()` |
| `BenchManager` | Bench slots, bench unit list, `GetEmptySlot()` |
| `VfxPoolManager` | Prefab-keyed GameObject pools; `Get / Return / Trim / ClearAll` |

Rule of thumb: if it needs Inspector references or Unity callbacks, it is a MonoBehaviour singleton;
if it is a plain registry, it is pure C#.

### 2.3 Data assets (ScriptableObjects)

| Asset type | Purpose | Create menu |
|---|---|---|
| `UnitData` | One unit at one star level: stats, prefab, cost, `upgradeUnit`, synergies, skill, trail | `Scriptable Objects/UnitData` |
| `UnitPoolDatabase` | Tier-1 base champions + copies-per-cost table | `Scriptable Objects/UnitPoolDatabase` |
| `StageData` | Enemy list (unit + coordinate) + percent `enemyBuffs` | `Scriptable Objects/StageData` |
| `BaseSkill` subclasses | A unit's skill; `Execute(caster, ct)` | `Scriptable Objects/Skill/...` |
| `ProjectileData` | Projectile prefab/speed/behavior for projectile skills | `Scriptable Objects/Skill/ProjectileData` |
| `SynergyData` | Name, icons, `SynergyTier[]` (count thresholds + behaviors) | `Scriptable Objects/SynergyData` |
| `SynergyBehavior` subclasses | What a tier does (`Apply` / `Remove`) | `Scriptable Objects/Synergy/...` |
| `SynergyState` | **Runtime** shared state (active synergies) — one asset, written by `SynergyManager` | already exists |
| `AugmentData` | Card: name, description, icon, rarity, `unique`, `effects[]` | `Scriptable Objects/Augment/AugmentData` |
| `AugmentEffect` subclasses | Stat boost / synergy count / gold | `Scriptable Objects/Augment/...` |
| `AugmentPool` | Offer pool with rarity weights + editor collect | `Scriptable Objects/Augment/AugmentPool` |
| `ProbabilityTable` | Shop cost-tier odds per level (rows must sum to 100) | `Scriptable Objects/ProbabilityTable` |
| `SoundLibrary` | `SoundId → clip + params` | `Scriptable Objects/Audio/SoundLibrary` |
| `AntiStallConfig` | Timing for the anti-stall escalation | `Scriptable Objects/Combat/AntiStallConfig` |

One important oddity: **`SynergyState` is a ScriptableObject used as runtime state.** It persists between
editor play sessions, so `SynergyManager.Awake` clears it. Never treat it as authored data.

### 2.4 The unit (component composition)

A unit prefab has one `UnitController` plus six required sibling components (`[RequireComponent]`):

| Component | Responsibility |
|---|---|
| `UnitController` | Hub. Placement (hex or bench), `PerformAttack`, `TakeDamage`, `Die`, `CastSkillAsync`, skill-VFX lifecycle, events |
| `UnitStats` | Copies base values from `UnitData`; HP/MP/shield; additive stat modifiers; multiplicative debuffs; synergy + augment buff bookkeeping |
| `UnitAI` | Combat FSM: `Idle → Moving / Attacking / Casting → Dead`, plus `Stunned`. Target search, path following |
| `UnitAnimator` | Animator parameters and triggers, attack/skill speed |
| `UnitMovement` | Tile-to-tile lerps, facing |
| `UnitVisuals` | Fire point / hit box, bullet trails, heal/shield VFX, attack & skill sounds, animation-event signals |
| `UnitCCHandler` | Stun / taunt state and CC VFX |

**Spawn flow** — always go through `UnitSpawner.SpawnUnit(data, tile, team, register)`:

```
Instantiate(data.unitPrefab) → controller.Initialize(data, tile, team)
  → (register ? UnitManager.AddUnit) → controller.NotifySpawned()  // fires OnUnitSpawned
```

`register: false` is used for bench placement and enemy previews (they are registered later, at battle
start). `OnUnitSpawned` is what `BarManager` (HP/MP bars) and `SynergyManager` listen to, and it fires
*after* registration on purpose.

**Stat layering** (in `UnitStats`):

1. **Base** — copied from `UnitData` on `Initialize` / `ResetStats`.
2. **Additive modifiers** — `ApplyStatModifier(stat, percent)` adds `percent%` *of the base value*. Because
   every delta is relative to base, `Apply(+20)` and `Apply(-20)` cancel exactly. This is why every
   `SynergyBehavior.Remove` just applies the negative.
3. **Multiplicative debuffs** — `ApplyStatDebuff / RemoveStatDebuff` keep a separate factor per stat.
   Read through the `CurrentXxx` properties to get the final value.

Synergy tiers are tracked per unit in `appliedSynergyTiers`; augment boosts are reconciled idempotently
via `SetAugmentBoosts` (removes the last snapshot, applies the new aggregate).

**Death vs. reset** — enemies are destroyed; player units are `SetActive(false)` after 3 s and revived by
`RoundManager.RestorePlayerPositions` → `ResetForNewRound()` (AI reset, stats reset, skill VFX returned).

### 2.5 Unit AI (FSM)

`UnitAI` subscribes to `BattleManager.OnBattleStart` (bench units skip it). Each state is an async
UniTask loop owned by a fresh `CancellationTokenSource` (`ResetToken()`); entering any state cancels the
previous one.

- `EnterIdleState` — `FindClosestTarget()` (hex distance, tie-break: shorter attack range), then attack if in
  range else move.
- `MoveAsync` — one tile at a time: re-validate target → re-check range → pick best adjacent tile →
  `Pathfinder.FindPath` (A*, `HexCoordCal`) → step. Blocked paths wait a frame and retry.
- `AttackAsync` — attack on a cadence derived from `CurrentAttSpd`; `PerformAttack` fires trails and applies
  damage when the last bullet lands; MP gained on attack and on hit.
- `EnterCastState` — when `Stats.CanCastSkill()` (MP full): look at target, `CastSkillAsync`, 0.3 s
  post-cast delay, back to Idle.
- `Stunned` / `Dead` cancel the AI outright.

### 2.6 Tiles and the board

`BaseTile` (abstract: `IsOccupied`, `GetCoordinate()`) → `TileScript` (battlefield hex, cube coords,
neighbors, `movementCost`) and `BenchTileScript` (bench slot). Every placement API takes a `BaseTile` so
board ↔ bench moves share code. `HexGridLayout` builds the grid at startup and registers tiles with
`TileManager`; `RoundManager.Start` waits 0.5 s plus the map intro before placing anything.

Stage spawn coordinates are offset `Vector2Int`s looked up through `TileManager.GetTile`. Player placement
is restricted to the player half by `UnitPlacer.playerZoneMaxRow`.

### 2.7 Round cycle in detail (`RoundManager`)

```
Start:      wait tiles → wait MapIntro → spawn starting bench units (pool TryAcquire) → round = 1
            → (once BottomBar intro is done) preview enemies → MaybeOfferAugment
BeginBattle: guards (phase == Preparation, not transitioning, field ≤ capacity)
            → UiTransition SFX → bar drains → SavePlayerUnitPositions → RegisterPreviewEnemies
            → BattleManager.StartBattle
OnBattleEnd: wait 5 s → ClearEnemyUnits (ClearSkillVfx first) → TileManager.ClearAllOccupied
            → RestorePlayerPositions (snapshot units back + reclaim orphans, e.g. mid-battle merges)
            → TrailPoolManager.Trim / VfxPoolManager.Trim
            → GrantInterest → GrantTurnGold → GrantRoundIncome (synergy) → GrantRoundExp
            → shop.RefreshForNewRound + AddFreeReroll → round++
            → (round > stages.Length ? GameClearUI.Show : preview next stage → ResetBattle → MaybeOfferAugment)
```

`ForceSetRound(n)` (Inspector context menu **Debug: Force Set Round**, or edit `currentRound` in Play mode)
safely jumps to any stage. Use it constantly when testing late-stage content.

### 2.8 Events — who talks to whom

| Event | Publisher | Typical subscribers |
|---|---|---|
| `BattleManager.OnBattleStart` (static) | phase → Battle | `UnitAI`, `AntiStallController`, UI |
| `BattleManager.OnBattleEnd(Team)` (static) | phase → Result | `RoundManager`, result stingers, anti-stall stop |
| `BattleManager.OnPreparationStart` (static) | `ResetBattle()` | `MergeManager.CheckAllMerges`, UI |
| `UnitController.OnUnitSpawned` (static) | `NotifySpawned()` | `BarManager`, `SynergyManager` |
| `PlayerManager.OnGoldChanged / OnExpChanged / OnLevelChanged` (static) | economy | HUD, `BoardManager` |
| `UnitPool.OnPoolChanged` (static) | acquire/return | shop UI |
| `AugmentManager.OnAugmentsChanged` (static) | choose/clear | `SynergyManager.Recalculate` |
| `BoardManager.OnCapacityChanged` (static) | capacity inputs | HUD |
| `AntiStallController.OnTriggered(int)` (static) | stall timer | buff / light / audio reactors |
| `SynergyState.OnSynergyChanged` (instance on SO) | `UpdateEntries` | every `UnitStats`, synergy UI, `BoardManager` |
| `UnitStats.OnHpChanged / OnMpChanged / OnShieldChanged / OnAttSpdChanged / OnHealed` | per unit | that unit's bars and visuals |
| `UnitController.OnBenchState / OnAttackHit / OnSkillHit / OnBeforeTakeDamage / OnBeforeAttack / OnBeforeSkillCast` | per unit | synergy recalc, event-trigger synergies |
| `ShopManager.OnShopChanged / OnLockChanged / OnFreeRerollChanged` | shop | shop UI |

Convention: subscribe in `OnEnable`, unsubscribe in `OnDisable` — **except** UI bars, which unsubscribe in
`OnDestroy` because bench units are toggled with `SetActive(false)` and must keep their subscriptions.

### 2.9 Synergy pipeline

`SynergyManager.Recalculate()` is the single rebuild that keeps the board consistent. It runs on unit spawn,
bench ↔ field moves, sells from the board, and augment changes:

1. Strip synergy buffs from every fielded player unit.
2. Tally synergies over fielded player units — **each champion name counts once**, so three copies or a
   Star 2 don't double count.
3. Fold in augment synergy bonuses (`SynergyCountAugmentEffect`) — this can activate a synergy from zero.
4. Write entries to `SynergyState` → `OnSynergyChanged` → each `UnitStats.OnSynergyChanged` applies the new
   tier's behaviors and removes the old tier's.
5. Apply the aggregated augment stat boosts to every fielded player unit (`SetAugmentBoosts`).

Behaviors are ScriptableObjects with a symmetric `Apply(unit)` / `Remove(unit)`:

| Behavior | Effect |
|---|---|
| `StatBoostBehavior` | Percent boosts on the synergy's own units |
| `GlobalStatBoostBehavior` | Percent boosts on the whole team |
| `GoldPerRoundBehavior` | Extra gold each round (`CalculateRoundIncome`) |
| `BoardCapacityBehavior` | Extra field slots (`CalculateBoardBonus`) |
| `DistanceAttackBonus` | Damage scaled by distance to target |
| `AttackTriggerDamage / AttackTriggerSkillDamage / AttackTriggerStun / DamageTriggerShield` | `EventTriggerBehavior` subclasses — subscribe to `OnAttackHit` / `OnSkillHit` / `OnBeforeTakeDamage` |

Synergy buffs apply to **player units only**. Enemies get their strength from `StageData.enemyBuffs`.

### 2.10 Augment pipeline

- `RoundManager.MaybeOfferAugment` → `AugmentSelectUI.Offer(3)` on augment rounds (default 1, 3, 5).
- `AugmentPool.Roll(count, owned)` samples without replacement, weighted by rarity
  (Common 10 / Bronze 5 / Silver 3 / Gold 2 by default), skipping owned `unique` augments.
- Picking calls `AugmentManager.Choose` → runs each effect's one-shot `OnAcquire()` → fires
  `OnAugmentsChanged` → `SynergyManager.Recalculate` re-applies recurring effects.

Effect types: `StatAugmentEffect` (recurring team stat %, aggregated additively), `SynergyCountAugmentEffect`
(recurring +N to a synergy), `GoldAugmentEffect` (one-shot gold). Card color comes from
`AugmentRarity.ToColor()`.

### 2.11 Shop, pool, and merging

- **Pool** — `UnitPool` seeds from `UnitPoolDatabase`: each base champion gets `copies` for its cost tier;
  every star variant maps back to its base (`baseOf`). Showing a unit in the shop **reserves** a copy
  (`TryAcquire`); rerolling returns unshown copies; buying keeps the copy with the unit; selling returns
  `CopiesFor(star)` copies. Merges conserve copies (3 → 1 unit worth 3).
- **Roll** — `ProbabilityTable.RollCostTier(level)` picks a cost tier; `UnitPool.GetRandomAvailableUnit(cost,
  excluded)` picks a base weighted by available copies. Champions the player has already maxed to 3★ are
  excluded (`ShopManager.CollectMaxedBases`).
- **Purchase** — needs an empty bench slot *or* a merge-on-buy; pays `cost`; spawns onto the bench with
  `register: false`; then `MergeManager.CheckMerge`.
- **Merge** — 3 identical `UnitData` across board + bench → destroy them, fly projectiles
  (`MergeVfxManager`), spawn `upgradeUnit` on the anchor tile (prefers a fielded copy), cascade. During
  Battle only bench copies count; during Result no merges happen.
- **Lock** — holds the shop through one round refresh.

### 2.12 VFX and object pooling

`VfxPoolManager.Get(prefab, pos, rot)` / `Return(prefab, instance)`. Pools auto-expand by 2 and are
`Trim()`med at round end (pools untouched this round are destroyed). Persistent skill VFX (auras) must be
registered with `caster.RegisterSkillVfx(prefab, instance)` so the caster returns them on death / round
reset — do **not** try to return them from inside the skill's async flow (the cast token is disposed the
moment `Execute` returns). Bullet trails use `TrailPoolManager` with per-unit pre-warm from `UnitData`.

### 2.13 Audio

`SoundManager.Instance.PlayUi(SoundId.X)` / `Play(SoundId.X)` / `PlayAt(id, pos)` / `PlayMusic(id)`.
`SoundId` is an explicit-valued enum with ranges: UI 10–49, BGM 50–99, shared SFX 100+. Per-unit attack and
skill clips are **not** in the catalog; they live on `UnitVisuals`. `SoundLibrary.asset` maps ids to clips.

---

## Part 3 — Development Workflow

### 3.1 First-day setup

1. Install **Unity 6000.3.10f1** (exact version — do not upgrade the project).
2. Clone `main` and open the project. Let it import.
3. Install **unity-cli** (`irm https://raw.githubusercontent.com/youngwoocho02/unity-cli/master/install.ps1 | iex`
   on Windows). The Unity-side connector is already in `Packages/manifest.json`.
4. In Unity: **Edit → Preferences → General → Interaction Mode → No Throttling**, so CLI commands run while
   the editor is unfocused.
5. Verify:
   ```bash
   unity-cli status
   unity-cli editor refresh --compile
   unity-cli console --filter error      # expect: no errors
   ```
6. Read `CLAUDE.md` (project rules) and skim the top of `unity-cli/ChangeLog.md` to see what changed last.
7. Open `Assets/Scenes/Title.unity` and press Play for the full flow, or open `Main.unity` directly to skip
   the title.

### 3.2 The daily loop

```
edit code / assets
  → unity-cli editor refresh --compile      # required after every code change
  → unity-cli console --filter error        # must be empty
  → unity-cli editor play --wait            # test
  → unity-cli console --filter all          # read your logs
  → unity-cli editor stop
  → append an entry to unity-cli/ChangeLog.md
```

Rules that are not optional:

- **Compile via unity-cli after every code change** and check for errors before moving on.
- **Scene changes go through `unity-cli exec`** (or the editor), never by hand-editing `.unity` YAML. If you
  must text-edit an asset, run `unity-cli reserialize <path>` afterwards.
- **Record every finished task in `unity-cli/ChangeLog.md`** (newest at top, `## Session — <title>`, bullets
  per file, plus how you verified).
- Check `unity-cli/ToDoList.md` for planned work when it exists (it is git-ignored, so it may be absent on a
  fresh clone).
- **English only** in code, comments, logs, and docs.

`unity-cli exec` runs real C# on the main thread:

```bash
# expressions auto-return
unity-cli exec "Object.FindFirstObjectByType<RoundManager>().CurrentRound"
# multi-statement needs an explicit return; use --usings for namespaces
unity-cli exec "var rm = Object.FindFirstObjectByType<RoundManager>(); rm.ForceSetRound(20); return rm.CurrentRound;"
```

Known quirks: the connection drops during domain reloads and `AssetDatabase.SaveAssets()` (you see
"connection closed" or "cannot connect") — the change still applied; re-query to confirm. Private serialized
fields need reflection. `FindObjectsByType<T>` needs the two-argument overload
`(FindObjectsInactive.Include, FindObjectsSortMode.None)` if you want inactive objects.

### 3.3 Code style

Follow `Assets/Scripts/Unit/UnitAI.cs` as the reference file.

- `<summary>` is 1–2 lines. Say what it does, not how.
- Section dividers: `// Section Name //` (e.g. `// State Transitions //`, `// Roll //`).
- Inline comments are short and sit next to the code:
  `[SerializeField] private int rerollCost = 2; // gold per reroll`
- Fields: `[SerializeField] private camelCase`; expose with read-only properties (`public int Gold => gold;`).
- Events: `On<Thing>` (`OnHpChanged`), `Action` / `Action<T>`; static for global, instance for per-object.
- Async: `async UniTask` / `UniTaskVoid` with a `CancellationToken` parameter; fire-and-forget with `.Forget()`;
  cancel and dispose CTS in `OnDisable` / `OnDestroy`. New code never uses coroutines.
- Managers guard against missing peers (`if (SoundManager.Instance != null)`).
- Log prefix in brackets: `Debug.Log("[Shop] Not enough gold")`.

### 3.4 Naming conventions for assets

| Thing | Pattern | Example |
|---|---|---|
| Unit data | `{Faction}_{N}_Star{1\|2\|3}.asset` in `Data/Units/{Faction}/Star{n}/`; dual-synergy units insert the class or weapon: `{Faction}_{Class}_{N}_Star1` | `Chaos_1_Star1`, `Divinity_Enforcer_2_Star1`, `Innovation_AR_1_Star1` |
| Unit prefab | Same stem as the data, in `Resources/Prefabs/Units/{Faction}_Units/Star{n}/` | `Chaos_1_Star2.prefab` |
| Skill asset | `{F}{N}_{Type}` (F = faction initial); Elite-class units use `{F}_E{N}_` | `C1_AoeCone`, `C_E3_ProjectileSkill`, `C4_ProjectileData` |
| Skill VFX prefab | `{F}{N}_Skill` or `{F}{N}_Projectiles` in `Resources/Prefabs/Effect/Unit_Skills/` | `H1_Skill.prefab`, `I4_Projectiles.prefab` |
| Synergy | `Data/Synergies/{Synergy}/{korean name}.asset` + `Tier/{Initial}_Tier N.asset`; weapons under `Weapons/{Weapon}/` | `Synergies/Chaos/혼돈.asset`, `Chaos/Tier/C_Tier 1.asset` |
| Augment | `{Effect}_{Rarity}.asset` + `{Effect}_{Rarity}_Effect.asset`, in a rarity subfolder | `StatAugments/Bronze/Armor_Bronze.asset`, `SynergyAugments/Pistol_Silver.asset` |
| Stage | `Stage{N}.asset` in `Data/Stages/` | `Stage16.asset` |
| Sounds | `Ui_{Action}.wav` in `Resources/Sound/UI/`; ids `Ui{Action}` in `SoundId` | `Ui_Reroll.wav` → `SoundId.UiReroll` |
| Enemy-only units | `Data/Units/Enemy/Agent_{N}.asset` — never in the pool database | `Agent_1` |

Star-tier data share the same `unitName` (that is how synergy counting and merges recognize a champion).

### 3.5 Recipes

#### Recipe A — Add a new champion

1. **Data**: right-click `Assets/Data/Units/{Faction}/Star1` → *Create → Scriptable Objects → UnitData*.
   Fill `unitName` (Korean), `cost`, `sellPrice`, stats, `synergies`, `skill`, `bulletTrailPrefab`,
   `poolSize`, `mpGainOnAttack/OnHit`, `portrait`, `thumbnail`. Leave `starLevel = 1`.
2. **Prefab**: build the Star 1 prefab in `Resources/Prefabs/Units/{Faction}_Units/Star1/`. It needs a
   `UnitController` (the other six components are added automatically) and child transforms **FirePoint**
   (muzzle), **HitBox** (chest), **UIAnchor** (above head). Assign `UnitController.uiAnchor`,
   `UnitStats.synergyState` (the shared `SynergyState.asset`), `UnitVisuals` fire point / hit box / sounds,
   and an Animator Override Controller with Idle / Move / Attack / Skill / Death clips
   (attack & skill clips: Loop Time off). Point `UnitData.unitPrefab` at it.
3. **Star 2 / 3**: use the editor batch tools in `Editor/StarUpGenerator.cs` through `unity-cli exec`:
   `StarUpGenerator.GenerateOne(star1Path, star2Path, null, 1.35f)` and `GenerateStar3One(star2Path,
   star3Path, null, 1.5f)` build the prefabs (armor pieces enabled, silver then gold trim, root scale
   1.2 / 1.35 / 1.5). Then create the Star 2 and Star 3 `UnitData` (copy Star 1, `starLevel` 2 / 3,
   HP and ATK ×1.7 per star, `unitPrefab` → the new prefab) and chain `upgradeUnit`: Star1 → Star2 → Star3
   → null. `WireStarData()` automates the Star2→Star3 half for existing folders.
4. **Pool**: add the **Star 1** data to `UnitPoolDatabase.baseUnits`. Without this the shop never rolls it and
   `UnitPool` warns "not in pool database".
5. **Verify** in Play: it appears in the shop at the right cost; HP/MP bars show; trails fly FirePoint →
   HitBox; buying 3 merges; synergy panel counts it; sell returns the right copies.

#### Recipe B — Add a skill

*Using an existing type*: create the asset under `Assets/Data/Skills/{Faction}/` from
*Scriptable Objects → Skill → …*, set `castTime` or `useAnimationEvent`, `animationSpd`, `canCrit`,
`castVfxPrefab`, type-specific fields, and assign it to `UnitData.skill`. MP thresholds come from
`UnitData.maxMp` and the MP-gain fields.

*New type*: subclass `BaseSkill`, add `[CreateAssetMenu(fileName = "X", menuName = "Scriptable Objects/Skill/XSkill")]`,
and implement `public override async UniTask<bool> Execute(UnitController caster, CancellationToken ct)`:

- Wait for the cast: `await caster.Visuals.WaitForSkillEvent(ct)` if `useAnimationEvent`, else
  `await UniTask.WaitForSeconds(castTime, cancellationToken: ct)`.
- After every await, bail if `caster.Stats.CurrentHp <= 0`, `ct.IsCancellationRequested`, or the phase is no
  longer `Battle`. Return `false` when the cast did not fire.
- Damage: `caster.Stats.CurrentAtt * caster.Stats.SkillDamageMultiplier * yourMultiplier`, then
  `caster.Stats.ApplyCrit(dmg, out _)` when `canCrit`. Apply with `target.TakeDamage(dmg, caster)` and call
  `caster.RaiseSkillHit(target, dmg)` so on-hit synergies trigger.
- VFX: `VfxPoolManager.Instance.Get(prefab, pos, rot)`; short-lived effects return themselves; anything that
  persists must go through `caster.RegisterSkillVfx(prefab, instance)`.
- Add a custom inspector in `Scripts/Editor/` only if the asset needs area previews (see `AoESkillEditor`).

#### Recipe C — Add a synergy

1. Create `SynergyData` in `Assets/Data/Synergies/{Name}/` (Korean `synergyName`, `icon`, `inactiveIcon`,
   `isWeapon` for weapon synergies, `description`).
2. Create behavior assets in `{Name}/Tier/` from *Scriptable Objects → Synergy → …* (e.g. `StatBoost`), one
   per tier.
3. Fill `tiers[]` with ascending `requiredCount`, a per-tier `description` (TMP rich text), tier icon, sort
   keys, and `behaviors[]`.
4. Add the synergy to every member's `UnitData.synergies` (all three star assets).
5. *New behavior type*: subclass `SynergyBehavior` (or `EventTriggerBehavior` for on-hit / on-damaged
   effects). `Remove` must undo exactly what `Apply` did. Stat effects use `ApplyStatModifier` with the
   negative percent; event effects unsubscribe. If the behavior contributes to income or board size, add a
   branch in `SynergyManager.CalculateRoundIncome` / `CalculateBoardBonus`.
6. Optional: `+1 {Synergy}` augment — see Recipe D with a `SynergyCountAugmentEffect`.

#### Recipe D — Add an augment

1. Create the effect asset (`StatAugmentEffect`, `SynergyCountAugmentEffect`, or `GoldAugmentEffect`) and
   the `AugmentData` next to it in the matching rarity folder (`StatAugments/{Rarity}/`,
   `GoldAugments/{Rarity}/`, or `SynergyAugments/`). Name both `{Effect}_{Rarity}` / `..._Effect`.
2. Set `augmentName` and `description` (Korean, numbers matching the effect), `icon`, `rarity`, `unique`,
   and `effects[]`. Current scaling for stat augments: Common ×1.0, Bronze ×1.4, Silver ×1.6, Gold ×2.0.
3. Make sure the folder is listed in `DefaultAugmentPool.asset → collectFolders`, then run the pool's
   context menu **Collect Augments From Folders** (Inspector ⋮ menu). Entries are sorted by rarity then name.
4. *New effect type*: subclass `AugmentEffect`. One-shot → override `OnAcquire()`. Recurring → add an
   aggregator in `AugmentManager` and apply it inside `SynergyManager.Recalculate` step 5, following
   `SetAugmentBoosts` (idempotent per unit).
5. Verify with `RoundManager` forced to an augment round and a few rerolls; check the card tint and that
   `Ui_Select` plays on pick.

#### Recipe E — Add a stage

1. Create `Assets/Data/Stages/Stage{N}.asset` (*Scriptable Objects → StageData*).
2. Fill `enemies[]`: `unitData` (any star of a player champion, or `Enemy/Agent_*`) and `spawnCoordinate`
   on the enemy half of the board. Reuse coordinates from the neighboring stage for a sane formation.
3. Optional `enemyBuffs[]` (percent stat boosts on every enemy). The existing ramp uses four levers: enemy
   count, Star 3 density, buffs, and stronger Star 3 picks — move one lever per stage, never all four.
4. Append it to `RoundManager.stages` in `Main.unity` via `unity-cli exec` (SerializedObject on the
   RoundManager, `InsertArrayElementAtIndex`, `EditorSceneManager.SaveScene`).
5. Test with **Debug: Force Set Round** → N, and also play N−1 → N to confirm the difficulty step.

#### Recipe F — Add a UI sound

1. Add the clip to `Resources/Sound/UI/` as `Ui_{Action}.wav`.
2. Add `Ui{Action} = <next free id in 10–49>` to `SoundId` with a short inline comment.
3. Add the entry to `SoundLibrary.asset`.
4. Call `SoundManager.Instance?.PlayUi(SoundId.Ui{Action})` at the trigger point.

### 3.6 Best practices and known gotchas

- **Percent modifiers are relative to base.** Always undo with the exact negative percent. Never set a
  `current*` stat directly from outside `UnitStats`.
- **Cancellation tokens**: check `ct.IsCancellationRequested` after every `await`. `CastSkillAsync` disposes
  its linked CTS as soon as `Execute` returns — nothing may keep awaiting that token afterwards (this was the
  cause of the lingering-aura bug).
- **Bench units are inactive AI.** `UnitAI.OnEnable` skips bench units; `SetActive(false)` is how dead player
  units wait for the next round. Any scan that should include them needs `FindObjectsInactive.Include`.
- **`register` matters.** Preview enemies and bench units use `register: false`; only fielded, fighting
  units belong in `UnitManager` rosters. `CheckBattleEnd` compares roster counts.
- **Player-only synergy buffs.** `UnitStats.OnSynergyChanged` returns early for non-player teams by design.
- **Pool copies are conserved.** The pool only changes in `TryAcquire` / `Return`. Merges do not return
  copies; sells return `CopiesFor(star)`.
- **`ProbabilityTable` rows must sum to 100** — `OnValidate` fixes them and logs a warning.
- **Shared SO state**: `SynergyState.asset` and `DefaultAugmentPool.asset` are shared across scenes. Do not
  add runtime state to other SOs without a reset path.
- **Trim after restore.** `VfxPoolManager.Trim()` runs after players have returned their VFX; keep that order.
- **Shop reservation**: a unit shown in the shop already holds its pool copy. Any new code path that removes a
  shop slot must `Return` the copy or hand it to a unit.
- **Debugging**: `UnitPool.debugPoolState` (Inspector) shows live pool counts; `RoundManager` → *Debug: Force
  Set Round*; `DebugSpawnButton` drops test units; `unity-cli console --filter all` reads the bracketed logs.

### 3.7 Definition of done

Before you call a task finished:

- [ ] `unity-cli editor refresh --compile` then `unity-cli console --filter error` shows no errors
- [ ] Tested in Play mode along the real path (shop → board → battle → next round), not only in isolation
- [ ] Console has no new warnings from your change
- [ ] New assets follow the naming table in 3.4 and live in the right folder
- [ ] New data is wired everywhere it is consumed (pool database, augment pool collect, `RoundManager.stages`)
- [ ] Code follows the `UnitAI.cs` comment style; English only
- [ ] `unity-cli/ChangeLog.md` has an entry describing the change and how you verified it

### 3.8 Where to look when something breaks

| Symptom | Start here |
|---|---|
| Unit never appears in the shop | `UnitPoolDatabase.baseUnits`, cost tier in `ProbabilityTable`, "not in pool database" warning |
| Synergy count wrong | `SynergyManager.Recalculate` step 2 (name-based dedupe), `UnitData.synergies` on every star asset |
| Buff stacks or never clears | `Apply`/`Remove` symmetry, `SetAugmentBoosts` snapshot, `ResetStats` |
| VFX left on the board after the round | `RegisterSkillVfx` missing, `Trim` order in `RoundManager` |
| Skill fires after death or in Preparation | missing post-`await` checks in `Execute` |
| Round won't start | `BeginBattle` guards: phase, `transitioning`, `FieldCount > Capacity` (console warns) |
| Unit orphaned after battle | `RestorePlayerPositions` reclaim log; check merge timing during Battle |
| unity-cli says "cannot connect" | domain reload in progress — wait, re-run `unity-cli status` |
