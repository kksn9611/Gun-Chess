using UnityEngine;
using System.Collections;

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

    [Tooltip("Cast time")]
    public float castTime = 1f;
    public float animationSpd = 1f;

    

    /// <summary>
    /// Execute the skill.
    /// </summary>
    public abstract IEnumerator Execute(UnitController caster);
}
