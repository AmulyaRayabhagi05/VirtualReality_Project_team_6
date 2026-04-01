using UnityEngine;
using TMPro;

public class NPCInteractable : MonoBehaviour
{
    [Header("NPC Identity")]
    public string npcName = "Merchant";

    [TextArea(3, 8)]
    public string systemPrompt =
        "You are a friendly merchant in a medieval fantasy town. " +
        "Speak in a warm, slightly old-fashioned tone. " +
        "Keep responses to 2-3 sentences. " +
        "You sell potions, herbs, and adventuring supplies.";

    [Header("Audio")]
    [Tooltip("AudioSource on this NPC — voice plays here so it sounds positional in VR")]
    public AudioSource npcAudioSource;

    [Header("World-space UI Panel")]
    [Tooltip("Root GameObject of the floating panel — shown when player is within range")]
    public GameObject  conversationPanel;
    public TMP_Text    npcNameLabel;   
    public TMP_Text    statusLabel;    
    public TMP_Text    transcriptLabel;  
    public UnityEngine.UI.Button startButton;
    public UnityEngine.UI.Button endButton;

    [Header("Proximity")]
    [Tooltip("Distance at which the floating panel becomes visible (metres)")]
    public float panelVisibleRange = 4f;

    private Transform _playerHead;

    private void Start()
    {
        var cam = Camera.main;
        if (cam != null) _playerHead = cam.transform;

        if (startButton != null) startButton.onClick.AddListener(OnStartPressed);
        if (endButton   != null) endButton.onClick.AddListener(OnEndPressed);

        if (conversationPanel != null) conversationPanel.SetActive(false);
        SetIdleState();
    }

    private void Update()
    {
        HandlePanelVisibility();
        BillboardPanel();
    }

    public void OnStartPressed()
    {
        if (ConversationManager.Instance == null)
        {
            Debug.LogError("[NPCInteractable] ConversationManager not found in scene!");
            return;
        }
        ConversationManager.Instance.StartConversation(this);
    }

    public void OnEndPressed()
    {
        ConversationManager.Instance?.EndConversation();
    }

    public void OnBecomeActive()
    {
        if (startButton != null) startButton.interactable = false;
        if (endButton   != null) endButton.interactable   = true;
        if (npcNameLabel != null) npcNameLabel.text = npcName;
        SetStatus("Hold A to speak");
        if (transcriptLabel != null) transcriptLabel.text = "";
    }

    public void OnBecomeInactive()
    {
        SetIdleState();
    }

    public void SetStatus(string msg)
    {
        if (statusLabel != null) statusLabel.text = msg;
    }

    public void SetTranscript(string msg)
    {
        if (transcriptLabel != null) transcriptLabel.text = msg;
    }

    private void SetIdleState()
    {
        if (startButton  != null) startButton.interactable  = true;
        if (endButton    != null) endButton.interactable     = false;
        if (npcNameLabel != null) npcNameLabel.text          = npcName;
        if (statusLabel  != null) statusLabel.text           = "Press Start to talk";
        if (transcriptLabel != null) transcriptLabel.text    = "";
    }

    private void HandlePanelVisibility()
    {
        if (conversationPanel == null || _playerHead == null) return;
        float dist = Vector3.Distance(transform.position, _playerHead.position);
        bool show = dist <= panelVisibleRange;
        if (conversationPanel.activeSelf != show)
            conversationPanel.SetActive(show);
    }

    private void BillboardPanel()
    {
        if (conversationPanel == null || !conversationPanel.activeSelf || _playerHead == null) return;
        Vector3 dir = conversationPanel.transform.position - _playerHead.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            conversationPanel.transform.rotation = Quaternion.LookRotation(dir);
    }
}