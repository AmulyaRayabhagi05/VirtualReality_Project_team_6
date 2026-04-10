using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public AudioMixer audioMixer;

    public float MasterVolume { get; private set; }
    public float MusicVolume  { get; private set; }
    public float SFXVolume    { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void SetMasterVolume(float sliderValue)
    {
        MasterVolume = sliderValue;
        ApplyToMixer("MasterVolume", sliderValue);
    }

    public void SetMusicVolume(float sliderValue)
    {
        MusicVolume = sliderValue;
        ApplyToMixer("MusicVolume", sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        SFXVolume = sliderValue;
        ApplyToMixer("SFXVolume", sliderValue);
    }

    private void ApplyToMixer(string paramName, float linearValue)
    {
        float clamped = Mathf.Clamp(linearValue, 0.0001f, 1f);
        float dB = Mathf.Log10(clamped) * 20f;
        audioMixer.SetFloat(paramName, dB);
    }
}