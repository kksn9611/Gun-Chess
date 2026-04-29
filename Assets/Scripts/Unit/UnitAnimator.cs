using UnityEngine;

[RequireComponent(typeof(UnitController))]
[RequireComponent(typeof(Animator))]
public class UnitAnimator : MonoBehaviour
{
    private Animator animator;
    private UnitAI ai;
    private UnitStats stats;
    [SerializeField] private float attackAnimLength = 1.0f;
    [SerializeField] private float skillAnimLength = 1.0f;

    public float SkillAnimLength => skillAnimLength;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ai = GetComponent<UnitAI>();
        stats = GetComponent<UnitStats>();
        ai.OnStateChanged += OnStateChanged;
        stats.OnAttSpdChanged += OnAttSpdChanged;
    }

    private void OnStateChanged(UnitState state)
    {
        animator.SetInteger("State", (int)state);
    }

    private void OnAttSpdChanged(float attSpd)
    {
        if (attSpd <= 0f) attSpd = 0.01f;
        float timePerAttack = 1f / attSpd;
        float speedMultiplier = attackAnimLength / timePerAttack;

        animator.SetFloat("AttSpd", speedMultiplier);
    }
    /// <summary>Set skill animation speed based on cast time.</summary>
    public void SetSkillSpeed(float castSpd)
    {
        if (castSpd <= 0f) castSpd = 0.01f;
        animator.SetFloat("SkillSpd", castSpd);
    }
    /// <summary>Fire attack animation once per attack.</summary>
    public void PlayAttack()
    {
        animator.SetTrigger("Attack");
    }
    /// <summary> Cast skill animation. </summary>
    public void PlaySkill()
    {
        animator.SetTrigger("Skill");
    }
    public void SlowAnimation()
    {
        animator.speed = 0f;
    }
    public void ResumeAnimation()
    {
        animator.speed = 1f;
    }
    /// <summary>Clear stale triggers to prevent animation conflicts on death.</summary>
    public void ResetTriggers()
    {
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Skill");
    }


    private void OnDestroy()
    {
        if (ai != null)
            ai.OnStateChanged -= OnStateChanged;
        if (stats != null)
            stats.OnAttSpdChanged -= OnAttSpdChanged;
    }
}
