using System.Collections.Concurrent;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class ProximityVoiceChat : NetworkBehaviour
{
    [SerializeField] private float maxHearingDistance = 5f;
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private float sendInterval = 0.05f;

    private AudioSource _audioSource;
    private AudioClip _micClip;
    private int _lastMicPos;
    private float _sendTimer;

    private bool _isOwnerCached;

    private readonly ConcurrentQueue<float> _receiveBuffer = new ConcurrentQueue<float>();

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.spatialBlend = 1f;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.minDistance = 1f;
        _audioSource.maxDistance = maxHearingDistance;
        _audioSource.loop = true;
        _audioSource.volume = 1f;
        _audioSource.playOnAwake = false;
    }

    public override void OnNetworkSpawn()
    {
        _isOwnerCached = IsOwner;

        if (IsOwner)
        {
            StartMicrophone();
        }
        else
        {
            AudioClip silentClip = AudioClip.Create("VoiceOutput", sampleRate, 1, sampleRate, false);
            _audioSource.clip = silentClip;
            _audioSource.Play();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && Microphone.IsRecording(null))
            Microphone.End(null);

        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();
    }

    private void StartMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[ProximityVoiceChat] No microphone detected.");
            return;
        }

        _micClip = Microphone.Start(null, true, 1, sampleRate);
        _lastMicPos = 0;
        Debug.Log($"[ProximityVoiceChat] Microphone started on: {Microphone.devices[0]}");
    }

    private void Update()
    {
        if (!IsOwner || _micClip == null || !Microphone.IsRecording(null)) return;

        _sendTimer += Time.deltaTime;
        if (_sendTimer < sendInterval) return;
        _sendTimer = 0f;

        int currentPos = Microphone.GetPosition(null);
        if (currentPos < 0) return;

        int samplesAvailable = currentPos >= _lastMicPos
            ? currentPos - _lastMicPos
            : (_micClip.samples - _lastMicPos) + currentPos;

        if (samplesAvailable <= 0) return;

        float[] samples = new float[samplesAvailable];
        _micClip.GetData(samples, _lastMicPos);
        _lastMicPos = currentPos;

        SendVoiceServerRpc(samples);
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (_isOwnerCached) return;

        for (int i = 0; i < data.Length; i += channels)
        {
            _receiveBuffer.TryDequeue(out float sample);
            for (int c = 0; c < channels; c++)
                data[i + c] = sample;
        }
    }

    [ServerRpc]
    private void SendVoiceServerRpc(float[] samples)
    {
        BroadcastVoiceClientRpc(samples);
    }

    [ClientRpc]
    private void BroadcastVoiceClientRpc(float[] samples)
    {
        if (IsOwner) return;

        if (_receiveBuffer.Count > sampleRate)
            while (_receiveBuffer.TryDequeue(out _)) { }

        foreach (float s in samples)
            _receiveBuffer.Enqueue(s);
    }
}
