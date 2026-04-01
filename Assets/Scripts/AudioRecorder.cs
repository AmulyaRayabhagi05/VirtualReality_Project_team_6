using System;
using UnityEngine;

/// <summary>
/// Handles microphone recording and converts the result to a WAV byte array
/// ready to POST to the Whisper API.
/// 
/// Usage:
///   AudioRecorder.Instance.StartRecording();
///   AudioRecorder.Instance.StopRecording(out byte[] wav);
/// </summary>
public class AudioRecorder : MonoBehaviour
{
    public static AudioRecorder Instance { get; private set; }

    [Header("Recording settings")]
    public int sampleRate    = 16000;   // Whisper works best at 16 kHz
    public int maxDuration   = 30;      // seconds — safety cap

    private AudioClip _clip;
    private bool      _isRecording;
    private string    _device;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Pick the first available microphone
        if (Microphone.devices.Length > 0)
            _device = Microphone.devices[0];
        else
            Debug.LogWarning("[AudioRecorder] No microphone found!");
    }

    public bool IsRecording => _isRecording;

    /// <summary>Start capturing from the microphone.</summary>
    public void StartRecording()
    {
        if (_isRecording || _device == null) return;
        _clip = Microphone.Start(_device, false, maxDuration, sampleRate);
        _isRecording = true;
        Debug.Log("[AudioRecorder] Recording started");
    }

    /// <summary>
    /// Stop recording and return the captured audio as a WAV byte array.
    /// Returns null if nothing was recorded.
    /// </summary>
    public byte[] StopRecording()
    {
        if (!_isRecording || _device == null) return null;

        int position = Microphone.GetPosition(_device);
        Microphone.End(_device);
        _isRecording = false;

        if (position <= 0)
        {
            Debug.LogWarning("[AudioRecorder] No audio captured.");
            return null;
        }

        // Trim the clip to the actual recorded length
        float[] samples = new float[position * _clip.channels];
        _clip.GetData(samples, 0);

        byte[] wav = EncodeWAV(samples, _clip.channels, sampleRate);
        Debug.Log($"[AudioRecorder] Recorded {position} samples → {wav.Length} bytes WAV");
        return wav;
    }

    // ── WAV encoder ──────────────────────────────────────────────────────────
    // Produces a standard PCM 16-bit WAV file in memory.

    private static byte[] EncodeWAV(float[] samples, int channels, int hz)
    {
        int sampleCount  = samples.Length;
        int byteCount    = sampleCount * 2;          // 16-bit = 2 bytes per sample
        byte[] wav       = new byte[44 + byteCount]; // 44-byte WAV header

        // RIFF header
        WriteString(wav,  0, "RIFF");
        WriteInt32(wav,   4, 36 + byteCount);        // file size - 8
        WriteString(wav,  8, "WAVE");
        WriteString(wav, 12, "fmt ");
        WriteInt32(wav,  16, 16);                    // PCM chunk size
        WriteInt16(wav,  20, 1);                     // PCM format
        WriteInt16(wav,  22, (short)channels);
        WriteInt32(wav,  24, hz);                    // sample rate
        WriteInt32(wav,  28, hz * channels * 2);     // byte rate
        WriteInt16(wav,  32, (short)(channels * 2)); // block align
        WriteInt16(wav,  34, 16);                    // bits per sample
        WriteString(wav, 36, "data");
        WriteInt32(wav,  40, byteCount);

        // Sample data — convert float [-1,1] to int16
        int offset = 44;
        foreach (float s in samples)
        {
            short v = (short)Mathf.Clamp(s * 32767f, short.MinValue, short.MaxValue);
            wav[offset++] = (byte)(v & 0xFF);
            wav[offset++] = (byte)(v >> 8);
        }
        return wav;
    }

    private static void WriteString(byte[] b, int offset, string s)
    {
        foreach (char c in s) b[offset++] = (byte)c;
    }
    private static void WriteInt32(byte[] b, int offset, int v)
    {
        b[offset]   = (byte)(v);
        b[offset+1] = (byte)(v >> 8);
        b[offset+2] = (byte)(v >> 16);
        b[offset+3] = (byte)(v >> 24);
    }
    private static void WriteInt16(byte[] b, int offset, short v)
    {
        b[offset]   = (byte)(v);
        b[offset+1] = (byte)(v >> 8);
    }
}