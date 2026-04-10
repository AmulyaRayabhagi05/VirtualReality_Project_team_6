using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Haptic vibration via XR controllers (Meta Quest, SteamVR, etc.)
/// No serial/Arduino dependency — no System.IO.Ports needed.
///
/// If you DO need Arduino serial later, go to:
/// Edit → Project Settings → Player → Other Settings
/// → Api Compatibility Level → .NET Framework
/// Then re-add the serial code.
/// </summary>
public class HapticManager : MonoBehaviour
{
    [Header("XR Settings")]
    [Tooltip("Which controller to vibrate")]
    public XRNode hapticNode = XRNode.RightHand;

    private Coroutine hapticRoutine;

    /// <summary>Trigger a vibration buzz for a given duration and intensity (0-1).</summary>
    public void TriggerVibration(float duration, float intensity = 1f)
    {
        if (hapticRoutine != null) StopCoroutine(hapticRoutine);
        hapticRoutine = StartCoroutine(VibrationRoutine(duration, intensity));
    }

    IEnumerator VibrationRoutine(float duration, float intensity)
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(hapticNode, devices);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            foreach (var device in devices)
                device.SendHapticImpulse(0, intensity, 0.1f);

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        } 
    }
}
