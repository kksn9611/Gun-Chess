using UnityEngine;

[RequireComponent(typeof(UnitController))]
[RequireComponent(typeof(Animator))]
public class UnitAnimator : MonoBehaviour
{
    private Animator animator;
    private UnitAI ai;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ai = GetComponent<UnitAI>();
        ai.OnStateChanged += OnStateChanged;
    }

    private void OnStateChanged(UnitState state)
    {
        animator.SetInteger("State", (int)state);
    }

    private void OnDestroy()
    {
        if (ai != null)
            ai.OnStateChanged -= OnStateChanged;
    }
}
