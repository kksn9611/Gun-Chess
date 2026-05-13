using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Base class for all skills.
/// Created as ScriptableObject and assigned to UnitData skill slot.
/// Each skill overrides Execute().
/// </summary>
public abstract class BaseSkill : ScriptableObject
{
    [Header("Skill Info")]
    [Tooltip("Skill name")]
    public string skillName = "Default Skill";

    [Tooltip("Skill description")]
    [TextArea(2, 4)]
    public string description = "";
    
    [Tooltip("Cast time (if not useAnimationEvent)")]
    public float castTime = 1f;
    public float animationSpd = 1f;

    [Tooltip("Wait for Animation Event instead of castTime delay")]
    public bool useAnimationEvent = false;

    [Tooltip("Whether this skill can critically hit")]
    public bool canCrit = true;



    /// <summary>
    /// Execute the skill. Returns true if the skill fired, false if canceled.
    /// </summary>
    public abstract UniTask<bool> Execute(UnitController caster, CancellationToken ct = default);
}
