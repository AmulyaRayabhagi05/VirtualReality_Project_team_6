using System.Collections;
using UnityEngine;

public class HapticManager : MonoBehaviour
{
    public void TriggerVibration(float duration, float intensity = 1f)
    {
        StartCoroutine(VibrateRoutine(duration, intensity));
    }

    IEnumerator VibrateRoutine(float duration, float intensity)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
            TriggerAndroidVibration(duration, intensity);
#else
        Debug.Log("Haptic vibrate for {duration}s at {intensity} intensity");
#endif

        yield return new WaitForSeconds(duration);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void TriggerAndroidVibration(float duration, float intensity)
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator == null) return;

                int sdkInt;
                using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                    sdkInt = versionClass.GetStatic<int>("SDK_INT");

                long durationMs = (long)(duration * 1000);

                if (sdkInt >= 26)
                {
                    int amplitude = Mathf.RoundToInt(intensity * 255);
                    using (AndroidJavaClass vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        AndroidJavaObject effect = vibrationEffect.CallStatic<AndroidJavaObject>(
                            "createOneShot", durationMs, amplitude);
                        vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    vibrator.Call("vibrate", durationMs);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Haptic] Vibration failed: {e.Message}");
        }
    }
#endif
}