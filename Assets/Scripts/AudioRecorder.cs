using System;
using UnityEngine;

public class AudioRecorder : MonoBehaviour
{
    public static AudioRecorder Instance { get; private set; }

    [Header("Recording settings")]
    public int sampleRate = 16000;
    public int maxDuration = 30;

    private AudioClip _clip;
    private bool _isRecording;
    private string _device;

    public bool IsRecording => _isRecording;
    public bool HasMicrophone => !string.IsNullOrEmpty(_device);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (Microphone.devices.Length > 0)
        {
            _device = Microphone.devices[0];
        }
        else
        {
            Debug.LogWarning("[AudioRecorder] No microphone found!");
        }
    }

    public void StartRecording()
    {
        if (_isRecording || _device == null)
        {
            return;
        }

        _clip = Microphone.Start(_device, false, maxDuration, sampleRate);
        _isRecording = true;
        Debug.Log("[AudioRecorder] Recording started");
    }

    public byte[] StopRecording()
    {
        if (!_isRecording || _device == null)
        {
            return null;
        }

        int position = Microphone.GetPosition(_device);
        Microphone.End(_device);
        _isRecording = false;

        if (position <= 0)
        {
            Debug.LogWarning("[AudioRecorder] No audio captured.");
            return null;
        }

        float[] samples = new float[position * _clip.channels];
        _clip.GetData(samples, 0);

        byte[] wav = EncodeWav(samples, _clip.channels, sampleRate);
        Debug.Log($"[AudioRecorder] Recorded {position} samples -> {wav.Length} bytes WAV");
        return wav;
    }

    public string GetUnavailableReason()
    {
        return HasMicrophone ? null : "No microphone found on device";
    }

    private static byte[] EncodeWav(float[] samples, int channels, int hz)
    {
        int sampleCount = samples.Length;
        int byteCount = sampleCount * 2;
        byte[] wav = new byte[44 + byteCount];

        WriteString(wav, 0, "RIFF");
        WriteInt32(wav, 4, 36 + byteCount);
        WriteString(wav, 8, "WAVE");
        WriteString(wav, 12, "fmt ");
        WriteInt32(wav, 16, 16);
        WriteInt16(wav, 20, 1);
        WriteInt16(wav, 22, (short)channels);
        WriteInt32(wav, 24, hz);
        WriteInt32(wav, 28, hz * channels * 2);
        WriteInt16(wav, 32, (short)(channels * 2));
        WriteInt16(wav, 34, 16);
        WriteString(wav, 36, "data");
        WriteInt32(wav, 40, byteCount);

        int offset = 44;
        foreach (float sample in samples)
        {
            short value = (short)Mathf.Clamp(sample * 32767f, short.MinValue, short.MaxValue);
            wav[offset++] = (byte)(value & 0xFF);
            wav[offset++] = (byte)(value >> 8);
        }

        return wav;
    }

    private static void WriteString(byte[] bytes, int offset, string text)
    {
        foreach (char c in text)
        {
            bytes[offset++] = (byte)c;
        }
    }

    private static void WriteInt32(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }

    private static void WriteInt16(byte[] bytes, int offset, short value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
    }
}
