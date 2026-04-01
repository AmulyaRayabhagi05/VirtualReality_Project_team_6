using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
public class ConversationManager : MonoBehaviour
{
    public static ConversationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("API Keys — never commit to source control!")]
    public string openAIKey   = "YOUR_OPENAI_API_KEY";
    public string deepSeekKey = "YOUR_DEEPSEEK_API_KEY";

    private const string WHISPER_URL    = "https://api.openai.com/v1/audio/transcriptions";
    private const string DEEPSEEK_URL   = "https://api.deepseek.com/chat/completions";
    private const string TTS_URL        = "https://api.openai.com/v1/audio/speech";
    private const string DEEPSEEK_MODEL = "deepseek-chat";
    private const string TTS_MODEL      = "tts-1";

    [Header("TTS Voice")]
    [Tooltip("OpenAI voice: alloy, echo, fable, onyx, nova, shimmer")]
    public string ttsVoice = "onyx";

    [Header("Input Manager Axis Names")]
    public string axisA  = "js10"; 
    public string axisB  = "js5";  
    public string axisX  = "js2"; 
    public string axisY  = "js3"; 
    public string axisOK = "js7";

    [Header("Settings")]
    public int maxHistoryTurns = 10;

    private NPCInteractable   _activeNPC;
    private List<ChatMessage> _history = new();
    private bool              _isPipelineRunning;
    private bool              _isRecording; 


    private void Update()
    {
        if (_activeNPC == null) return;

        if (!_isPipelineRunning)
        {
            if (Input.GetButtonDown(axisA) || Input.GetKeyDown(KeyCode.A))
            {
                AudioRecorder.Instance?.StartRecording();
                _isRecording = true;
                _activeNPC.SetStatus("Listening...");
            }

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

        if (Input.GetButtonDown(axisOK)  || Input.GetKeyDown(KeyCode.O))
        {
            EndConversation();
        }
    }

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

    public void EndConversation()
    {
        if (_activeNPC == null) return;
        TerminateActiveConversation();
    }

    private IEnumerator RunPipeline(byte[] wavBytes)
    {
        NPCInteractable npc = _activeNPC;

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

    private void TerminateActiveConversation()
    {
        if (_activeNPC == null) return;

        _isPipelineRunning = false;
        _isRecording = false;
        _history.Clear();

        if (AudioRecorder.Instance != null && AudioRecorder.Instance.IsRecording)
            AudioRecorder.Instance.StopRecording();

        if (_activeNPC.npcAudioSource != null && _activeNPC.npcAudioSource.isPlaying)
            _activeNPC.npcAudioSource.Stop();

        _activeNPC.OnBecomeInactive();
        _activeNPC = null;
        Debug.Log("[ConversationManager] Conversation terminated");
    }

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