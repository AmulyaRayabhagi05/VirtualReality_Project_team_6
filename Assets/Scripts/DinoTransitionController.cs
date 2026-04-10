using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the skeleton ↔ flesh dinosaur swap, driven by named Animator states.
///
/// ── ANIMATOR SETUP (one-time, in Unity Editor) ──────────────────────────────
///
/// On the SKELETON'S Animator Controller:
///   1. Open Parameters tab → Add Trigger "StartWalk"
///   2. Add Trigger "StartFall"
///   3. On the IdleGround → RiseUp transition:
///        • HasExitTime = OFF
///        • Add Condition: StartWalk
///      (RiseUp → Roar → RoarToWalk → Walk can keep their ExitTime auto-chains)
///   4. On the Walk state add a new transition Walk → Fall:
///        • HasExitTime = OFF
///        • Add Condition: StartFall
///   5. Fall → IdleGround can keep its ExitTime (plays to end, loops back)
///
/// On the FLESH DINO'S Animator Controller (if it has one):
///   • Add Trigger "StartFall"
///   • Add a Walk state and a Fall state that mirror the skeleton's
///   • Walk → Fall transition: HasExitTime OFF, condition StartFall
///
/// ── FLOW ────────────────────────────────────────────────────────────────────
///   Press 1:  Skeleton animator ON → StartWalk trigger →
///             IdleGround → RiseUp → Roar → RoarToWalk → Walk
///             Once Walk starts → hide skeleton, show flesh walking
///   Press 2:  StartFall trigger → flesh (or skeleton) plays Fall →
///             Everything resets to static skeleton, animator OFF
/// </summary>
public class DinoTransitionController : MonoBehaviour
{
    [Header("Dinosaur GameObjects")]
    public GameObject skeletonDino;
    public GameObject fleshDino;

    [Header("Animators")]
    public Animator skeletonAnimator;
    public Animator fleshAnimator;      // assign if flesh dino has its own animator

    [Header("Animator Trigger Names")]
    public string startWalkTrigger = "StartWalk";
    public string startFallTrigger = "StartFall";

    [Header("Timing")]
    [Tooltip("Extra seconds to stay in Walk before swap (let loop play a bit)")]
    public float walkSwapDelay = 0.3f;

    [Header("Button UI")]
    public PressButtonController buttonController;

    // ── state machine ───────────────────────────────────────────────
    private enum Phase { Idle, Transitioning, WalkingFleshed }
    private Phase phase = Phase.Idle;
    private Coroutine activeRoutine;

    // ── Unity ───────────────────────────────────────────────────────

    void Start()
    {
        skeletonDino.SetActive(true);
        fleshDino.SetActive(false);
        skeletonAnimator.enabled = true; 
        if (fleshAnimator) fleshAnimator.enabled = false;
        SetButton("Press");

    }

    // ── Called by PressButtonController ─────────────────────────────

    public void OnButtonPressed()
    {
        if (phase == Phase.Idle)
            Run(RoutinePress1());
        else if (phase == Phase.WalkingFleshed)
            Run(RoutinePress2());
        // ignore presses while Transitioning
    }

    // ── Press 1: idle skeleton → walk → swap to flesh ───────────────

    IEnumerator RoutinePress1()
    {
        phase = Phase.Transitioning;

        // Activate skeleton animator and fire the trigger
        skeletonDino.SetActive(true);
        fleshDino.SetActive(false);
        skeletonAnimator.enabled = true;
        skeletonAnimator.SetTrigger(startWalkTrigger);

        // Wait until the walk chain reaches the "Walk" state
        yield return WaitForState(skeletonAnimator, "Walk");
        yield return new WaitForSeconds(walkSwapDelay);

        // Swap: hide skeleton, show flesh
        skeletonDino.SetActive(false);
        fleshDino.SetActive(true);

        if (fleshAnimator != null)
        {
            fleshAnimator.enabled = true;
            fleshAnimator.Play("Walk", 0, 0f);   // jump straight into Walk
        }

        phase = Phase.WalkingFleshed;
        SetButton("Press");
    }

    // ── Press 2: flesh walks → fall → reset to static skeleton ──────

    IEnumerator RoutinePress2()
    {
        phase = Phase.Transitioning;

        if (fleshAnimator != null && fleshAnimator.enabled)
        {
            // Drive Fall on the flesh dino's own animator
            fleshAnimator.SetTrigger(startFallTrigger);
            yield return WaitForState(fleshAnimator, "Fall");
            yield return WaitForStateDone(fleshAnimator, "Fall");
            fleshAnimator.enabled = false;
        }
        else
        {
            // Flesh has no animator — hide it, show skeleton, play Fall there
            fleshDino.SetActive(false);
            skeletonDino.SetActive(true);
            skeletonAnimator.enabled = true;
            skeletonAnimator.SetTrigger(startFallTrigger);
            yield return WaitForState(skeletonAnimator, "Fall");
            yield return WaitForStateDone(skeletonAnimator, "Fall");
        }

        // Full reset
        fleshDino.SetActive(false);
        skeletonDino.SetActive(true);
        if (skeletonAnimator) skeletonAnimator.enabled = false;
        if (fleshAnimator) fleshAnimator.enabled = false;

        phase = Phase.Idle;
        SetButton("Press");
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// Waits until animator layer 0 is in the named state (polls each frame).
    IEnumerator WaitForState(Animator anim, string stateName, float timeout = 12f)
    {
        if (anim == null) yield break;
        yield return null;                          // let trigger propagate

        for (float t = 0; t < timeout; t += Time.deltaTime)
        {
            if (anim.GetCurrentAnimatorStateInfo(0).IsName(stateName))
                yield break;
            yield return null;
        }
        Debug.LogWarning($"[DinoController] Timed out waiting for state '{stateName}'");
    }

    /// Waits until the current clip's normalizedTime ≥ 1 (non-looping clip done).
    IEnumerator WaitForStateDone(Animator anim, string stateName)
    {
        if (anim == null) yield break;
        while (true)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(stateName)) yield break;
            if (!info.loop && info.normalizedTime >= 1f) yield break;
            yield return null;
        }
    }

    void Run(IEnumerator routine)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(routine);
    }

    void SetButton(string label)
    {
        if (buttonController) buttonController.SetButtonText(label);
    }
}