using System.Collections;
using UnityEngine;

public class DinoTransitionController : MonoBehaviour
{
    [Header("Dinosaur GameObjects")]
    public GameObject skeletonDino;
    public GameObject fleshDino;

    [Header("Animators")]
    public Animator skeletonAnimator;
    public Animator fleshAnimator;

    [Header("Animator Trigger Names")]
    public string startWalkTrigger = "StartWalk";
    public string startFallTrigger = "StartFall";

    [Header("Timing")]
    public float walkSwapDelay = 0.3f;

    [Header("Button")]
    public PressButtonController buttonController;

    private enum Phase { Idle, Transitioning, WalkingFleshed }
    private Phase phase = Phase.Idle;
    private Coroutine activeRoutine;

    void Start()
    {
        skeletonDino.SetActive(true);
        fleshDino.SetActive(false);
        skeletonAnimator.enabled = true; 
        if (fleshAnimator) fleshAnimator.enabled = false;
        SetButton("Press");

    }

    public void OnButtonPressed()
    {
        if (phase == Phase.Idle)
        {
            Run(RoutinePress1());
        }
        else if (phase == Phase.WalkingFleshed)
        {
            Run(RoutinePress2());
        }
    }


    IEnumerator RoutinePress1()
    {
        phase = Phase.Transitioning;

        skeletonDino.SetActive(true);
        fleshDino.SetActive(false);
        skeletonAnimator.enabled = true;
        skeletonAnimator.SetTrigger(startWalkTrigger);

        yield return WaitForState(skeletonAnimator, "Walk");
        yield return new WaitForSeconds(walkSwapDelay);

        skeletonDino.SetActive(false);
        fleshDino.SetActive(true);

        if (fleshAnimator != null)
        {
            fleshAnimator.enabled = true;
            fleshAnimator.Play("Walk", 0, 0f);
        }

        phase = Phase.WalkingFleshed;
        SetButton("Press");
    }

    IEnumerator RoutinePress2()
    {
        phase = Phase.Transitioning;

        if (fleshAnimator != null && fleshAnimator.enabled)
        {
            fleshAnimator.SetTrigger(startFallTrigger);
            yield return WaitForState(fleshAnimator, "Fall");
            yield return WaitForStateDone(fleshAnimator, "Fall");
            fleshAnimator.enabled = false;
        }else { 
            fleshDino.SetActive(false);
            skeletonDino.SetActive(true);
            skeletonAnimator.enabled = true;
            skeletonAnimator.SetTrigger(startFallTrigger);
            yield return WaitForState(skeletonAnimator, "Fall");
            yield return WaitForStateDone(skeletonAnimator, "Fall");
        }

        // reset
        fleshDino.SetActive(false);
        skeletonDino.SetActive(true);
        if (skeletonAnimator) skeletonAnimator.enabled = false;
        if (fleshAnimator) fleshAnimator.enabled = false;

        phase = Phase.Idle;
        SetButton("Press");
    }

    IEnumerator WaitForState(Animator anim, string stateName, float timeout = 12f)
    {
        if (anim == null)
        {
            yield break;
        }
        yield return null;

        for (float t = 0; t < timeout; t += Time.deltaTime)
        {
            if (anim.GetCurrentAnimatorStateInfo(0).IsName(stateName)) { 
                yield break;
            }
            yield return null;
        }
        Debug.LogWarning("DinoController Time out cause it was waiting for state '{stateName}'");
    }

    IEnumerator WaitForStateDone(Animator anim, string stateName)
    {
        if (anim == null)
        {
            yield break;
        }
        while (true)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(stateName))
            {
                yield break;
            }
            if (!info.loop && info.normalizedTime >= 1f) {
                yield break;
            }
            yield return null;
        }
    }

    void Run(IEnumerator routine)
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }
        activeRoutine = StartCoroutine(routine);
    }

    void SetButton(string label)
    {
        if (buttonController)
        {
            buttonController.SetButtonText(label);
        }
    }
}