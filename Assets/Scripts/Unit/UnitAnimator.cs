using Cysharp.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(UnitController))]
[RequireComponent(typeof(Animator))]
public class UnitAnimator : MonoBehaviour
{
    private Animator animator;
    private UnitAI ai;
    private UnitStats stats;
    [Header("These are need if only you need animation speed manually with this")]
    [SerializeField] private float attackAnimLength = 1.0f;
    [SerializeField] private float skillAnimLength = 1.0f;

    public float SkillAnimLength => skillAnimLength;
    private CancellationTokenSource cts;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ai = GetComponent<UnitAI>();
        stats = GetComponent<UnitStats>();
        ai.OnStateChanged += OnStateChanged;
        stats.OnAttSpdChanged += OnAttSpdChanged;
        cts = new CancellationTokenSource();
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
    /// <summary>Set skill animation speed.</summary>
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
    public void PlayStunAnimation()
    {
        animator.CrossFade("Stun",0.1f);
    }
    public void SlowAnimation(float time)
    {
        if (ai.CurrentState == UnitState.Dead) return;
        PauseAnimationAsync(time).Forget();
    }
    private async UniTaskVoid PauseAnimationAsync(float time)
    {
        animator.speed = 0.2f;
        await UniTask.WaitForSeconds(time, cancellationToken: cts.Token).SuppressCancellationThrow();
        animator.speed = 1f;
    }
    public void ResumeAnimation()
    {
        animator.speed = 1f;
    }

    /// <summary>
    /// Apply Root Motion On
    /// </summary>
    public void SetApplyRootMotion()
    {
        animator.applyRootMotion = true;
    }
    /// <summary>
    /// Apply Root Motion Off (Delay)
    /// </summary>
    public async UniTask ResetApplyRootMotion(int time)
    {
        await UniTask.Delay(time);
        animator.applyRootMotion = false;
    }
    /// <summary>
    /// Apply Root Motion Off
    /// </summary>
    public void ResetApplyRootMotion()
    {
        animator.applyRootMotion = false;
    }
    /// <summary>Clear stale triggers to prevent animation conflicts on death.</summary>
    public void ResetTriggers()
    {
        animator.applyRootMotion = true;
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
