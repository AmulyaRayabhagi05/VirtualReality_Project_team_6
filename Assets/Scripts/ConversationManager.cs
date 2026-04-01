using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Singleton that drives all NPC conversations in VR.
///
/// Input: Unity Legacy Input Manager — no Input System package required.
/// Add these entries in Edit → Project Settings → Input Manager (Type: Key or Mouse Button):
///
///   Name            Positive Button
///   ──────────────  ───────────────
///   VR_Button_A     joystick button 10
///   VR_Button_B     joystick button 5
///   VR_Button_X     joystick button 2
///   VR_Button_Y     joystick button 3
///   VR_Button_OK    joystick button 7
///
/// Push-to-talk: hold A (js10) — the same button on either controller.
/// The OK button (js7) ends the active conversation from the controller
/// without needing to reach the world-space panel.
///
/// Pipeline per turn:
///   Hold A → mic records → release A
///   → Whisper STT → DeepSeek chat → OpenAI TTS → NPC AudioSource plays reply
///
/// NPC switching:
///   StartConversation(newNPC) automatically ends any active conversation first.
/// </summary>
public class ConversationManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static ConversationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── API Keys ───────────────────────────────────────────────────────────────
    [Header("API Keys — never commit to source control!")]
    public string openAIKey   = "YOUR_OPENAI_API_KEY";
    public string deepSeekKey = "YOUR_DEEPSEEK_API_KEY";

    // ── Endpoints ──────────────────────────────────────────────────────────────
    private const string WHISPER_URL    = "https://api.openai.com/v1/audio/transcriptions";
    private const string DEEPSEEK_URL   = "https://api.deepseek.com/chat/completions";
    private const string TTS_URL        = "https://api.openai.com/v1/audio/speech";
    private const string DEEPSEEK_MODEL = "deepseek-chat";
    private const string TTS_MODEL      = "tts-1";

    // ── TTS Voice ──────────────────────────────────────────────────────────────
    [Header("TTS Voice")]
    [Tooltip("OpenAI voice: alloy, echo, fable, onyx, nova, shimmer")]
    public string ttsVoice = "onyx";

    // ── Input Manager axis names ───────────────────────────────────────────────
    // Change these only if you named your Input Manager entries differently.
    [Header("Input Manager Axis Names")]
    public string axisA  = "js10";    // js10 — push-to-talk (hold)
    public string axisB  = "js5";    // js5
    public string axisX  = "js2";    // js2
    public string axisY  = "js3";    // js3
    public string axisOK = "js7";   // js7 — end conversation from controller

    // ── Settings ───────────────────────────────────────────────────────────────
    [Header("Settings")]
    public int maxHistoryTurns = 10;

    // ── Private state ──────────────────────────────────────────────────────────
    private NPCInteractable   _activeNPC;
    private List<ChatMessage> _history = new();
    private bool              _isPipelineRunning;
    private bool              _isRecording;   // tracks whether we started a recording this press

    // ── Update — Input Manager polling ────────────────────────────────────────

    private void Update()
    {
        if (_activeNPC == null) return;

        // ── Push-to-talk (A button, js10) ─────────────────────────────────────
        if (!_isPipelineRunning)
        {
            // Button pressed — start recording
            if (Input.GetButtonDown(axisA) || Input.GetKeyDown(KeyCode.A))
            {
                AudioRecorder.Instance?.StartRecording();
                _isRecording = true;
                _activeNPC.SetStatus("Listening...");
            }

            // Button released — send audio through pipeline
            if ((Input.GetButtonUp(axisA)  || Input.GetKeyUp(KeyCode.A)) && _isRecording)
            {
                _isRecording = false;
                byte[] wav = AudioRecorder.Instance?.StopRecording();
                if (wav != null && wav.Length > 44)
                {
                    _isPipelineRunning = true;
                    StartCoroutine(RunPipeline(wav));
                }
                else
                {
                    _activeNPC.SetStatus("Hold A to speak");
                }
            }
        }

        // ── OK button (js7) — end conversation from controller ─────────────────
        if (Input.GetButtonDown(axisOK)  || Input.GetKeyDown(KeyCode.O))
        {
            EndConversation();
        }

        // ── B / X / Y are free for your own game logic ─────────────────────────
        // Example hooks — wire these up as needed:
        // if (Input.GetButtonDown(axisB)) { ... }
        // if (Input.GetButtonDown(axisX)) { ... }
        // if (Input.GetButtonDown(axisY)) { ... }
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Start a conversation with the given NPC.
    /// Automatically ends any currently active conversation first.
    /// Called from NPCInteractable.OnStartPressed().
    /// </summary>
    public void StartConversation(NPCInteractable npc)
    {
        if (npc == null) return;

        if (_activeNPC != null && _activeNPC != npc)
        {
            Debug.Log($"[ConversationManager] Switching from {_activeNPC.npcName} to {npc.npcName}");
            TerminateActiveConversation();
        }

        _activeNPC = npc;
        _history.Clear();
        _isPipelineRunning = false;
        _isRecording = false;

        _activeNPC.OnBecomeActive();
        Debug.Log($"[ConversationManager] Conversation started with {npc.npcName}");
    }

    /// <summary>
    /// End the active conversation.
    /// Called from NPCInteractable.OnEndPressed() or the OK button.
    /// </summary>
    public void EndConversation()
    {
        if (_activeNPC == null) return;
        TerminateActiveConversation();
    }

    // ── Pipeline ───────────────────────────────────────────────────────────────

    private IEnumerator RunPipeline(byte[] wavBytes)
    {
        NPCInteractable npc = _activeNPC;

        // Step 1 — Whisper STT
        npc.SetStatus("Transcribing...");
        string userText = null;
        yield return StartCoroutine(TranscribeAudio(wavBytes, r => userText = r));

        if (string.IsNullOrWhiteSpace(userText))
        {
            npc.SetStatus("Couldn't hear you — try again");
            _isPipelineRunning = false;
            yield break;
        }

        npc.SetTranscript($"You: {userText}");
        _history.Add(new ChatMessage("user", userText));
        Debug.Log($"[STT] {npc.npcName} heard: {userText}");

        // Step 2 — DeepSeek chat
        npc.SetStatus("Thinking...");
        string replyText = null;
        yield return StartCoroutine(GetDeepSeekReply(npc.systemPrompt, r => replyText = r));

        if (string.IsNullOrWhiteSpace(replyText))
        {
            npc.SetStatus("No reply — check DeepSeek key");
            _isPipelineRunning = false;
            yield break;
        }

        _history.Add(new ChatMessage("assistant", replyText));
        Debug.Log($"[DeepSeek] {npc.npcName} replied: {replyText}");

        // Step 3 — OpenAI TTS
        npc.SetStatus("Speaking...");
        AudioClip speech = null;
        yield return StartCoroutine(SynthesizeSpeech(replyText, c => speech = c));

        if (speech != null && npc.npcAudioSource != null)
        {
            npc.npcAudioSource.clip = speech;
            npc.npcAudioSource.Play();
            yield return new WaitWhile(() => npc.npcAudioSource.isPlaying);
        }
        else
        {
            Debug.LogWarning("[TTS] No clip returned or AudioSource missing on NPC.");
        }

        if (_activeNPC == npc)
        {
            npc.SetStatus("Hold A to speak");
            _isPipelineRunning = false;
        }
    }

    // ── Whisper STT ────────────────────────────────────────────────────────────

    private IEnumerator TranscribeAudio(byte[] wavBytes, System.Action<string> callback)
    {
        var form = new WWWForm();
        form.AddBinaryData("file",  wavBytes, "audio.wav", "audio/wav");
        form.AddField("model",    "whisper-1");
        form.AddField("language", "en");

        using var req = UnityWebRequest.Post(WHISPER_URL, form);
        req.SetRequestHeader("Authorization", $"Bearer {openAIKey}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            callback(ParseJsonField(req.downloadHandler.text, "text")?.Trim());
        else
        {
            Debug.LogError($"[Whisper] {req.error}\n{req.downloadHandler.text}");
            callback(null);
        }
    }

    // ── DeepSeek chat ──────────────────────────────────────────────────────────

    private IEnumerator GetDeepSeekReply(string systemPrompt, System.Action<string> callback)
    {
        var messages = new List<ChatMessage> { new ChatMessage("system", systemPrompt) };
        int start = Mathf.Max(0, _history.Count - maxHistoryTurns * 2);
        for (int i = start; i < _history.Count; i++) messages.Add(_history[i]);

        string json = BuildChatJson(messages);

        using var req = new UnityWebRequest(DEEPSEEK_URL, "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type",  "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {deepSeekKey}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            callback(ParseNestedJsonContent(req.downloadHandler.text)?.Trim());
        else
        {
            Debug.LogError($"[DeepSeek] {req.error}\n{req.downloadHandler.text}");
            callback(null);
        }
    }

    // ── OpenAI TTS ─────────────────────────────────────────────────────────────

    private IEnumerator SynthesizeSpeech(string text, System.Action<AudioClip> callback)
    {
        string json = "{\"model\":\"" + TTS_MODEL + "\","
                    + "\"voice\":\"" + ttsVoice   + "\","
                    + "\"input\":\""  + EscapeJson(text) + "\","
                    + "\"response_format\":\"mp3\"}";

        using var req = new UnityWebRequest(TTS_URL, "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerAudioClip(TTS_URL, AudioType.MPEG);
        req.SetRequestHeader("Content-Type",  "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {openAIKey}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            callback(DownloadHandlerAudioClip.GetContent(req));
        else
        {
            Debug.LogError($"[TTS] {req.error}\n{req.downloadHandler.text}");
            callback(null);
        }
    }

    // ── Internal ───────────────────────────────────────────────────────────────

    private void TerminateActiveConversation()
    {
        if (_activeNPC == null) return;

        _isPipelineRunning = false;
        _isRecording = false;
        _history.Clear();

        // Stop any in-progress mic recording — discard audio
        if (AudioRecorder.Instance != null && AudioRecorder.Instance.IsRecording)
            AudioRecorder.Instance.StopRecording();

        if (_activeNPC.npcAudioSource != null && _activeNPC.npcAudioSource.isPlaying)
            _activeNPC.npcAudioSource.Stop();

        _activeNPC.OnBecomeInactive();
        _activeNPC = null;
        Debug.Log("[ConversationManager] Conversation terminated");
    }

    // ── JSON helpers ───────────────────────────────────────────────────────────

    private static string BuildChatJson(List<ChatMessage> messages)
    {
        var sb = new StringBuilder();
        sb.Append("{\"model\":\"").Append(DEEPSEEK_MODEL).Append("\",");
        sb.Append("\"max_tokens\":512,\"messages\":[");
        for (int i = 0; i < messages.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append("{\"role\":\"").Append(messages[i].role)
              .Append("\",\"content\":\"").Append(EscapeJson(messages[i].content)).Append("\"}");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    private static string ParseJsonField(string json, string field)
    {
        string marker = "\"" + field + "\":\"";
        int idx = json.IndexOf(marker);
        if (idx < 0) return null;
        idx += marker.Length;
        var sb = new StringBuilder();
        bool esc = false;
        for (int i = idx; i < json.Length; i++)
        {
            char c = json[i];
            if (esc) { sb.Append(c == 'n' ? '\n' : c == 't' ? '\t' : c); esc = false; }
            else if (c == '\\') esc = true;
            else if (c == '"') break;
            else sb.Append(c);
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static string ParseNestedJsonContent(string json)
    {
        int idx = json.IndexOf("\"choices\"");
        if (idx < 0) return null;
        return ParseJsonField(json.Substring(idx), "content");
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"")
         .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");

    [System.Serializable]
    private class ChatMessage
    {
        public string role, content;
        public ChatMessage(string r, string c) { role = r; content = c; }
    }
}