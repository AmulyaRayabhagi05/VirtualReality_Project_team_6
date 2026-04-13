using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCInteractable : MonoBehaviour
{
    public string npcName = "Historian";

    [TextArea(3, 8)]
    public string systemPrompt = "";

    public AudioSource npcAudioSource;

    public GameObject conversationPanel;
    public TMP_Text npcNameLabel;
    public TMP_Text statusLabel;
    public TMP_Text transcriptLabel;

    public Button actionButton;
    public TMP_Text actionButtonLabel;

    public float panelVisibleRange = 4f;

    [Header("Reticle Input")]
    [SerializeField] private string actionButtonInput = "js2";
    [SerializeField] private KeyCode actionButtonKey = KeyCode.P;

    private Transform _playerHead;
    private bool _isActive;
    private float _lastActionTime = -10f;
    private const float ACTION_COOLDOWN = 0.5f;

    private void Start()
    {
        if (Camera.main != null)
            _playerHead = Camera.main.transform;

        if (actionButton != null)
        {
            // Remove before adding to prevent duplicate listeners if Start runs more than once
            actionButton.onClick.RemoveListener(OnActionButtonPressed);
            actionButton.onClick.AddListener(OnActionButtonPressed);
        }

        if (conversationPanel != null)
            conversationPanel.SetActive(false);

        SetIdleState();
    }

    private void Update()
    {
        // Retry each frame until the camera is found (handles VR rigs that initialise late)
        if (_playerHead == null && Camera.main != null)
            _playerHead = Camera.main.transform;

        HandlePanelVisibility();
        BillboardPanel();
    }

    private void OnActionButtonPressed()
    {
        // Prevent double-invocation from multiple input sources firing in the same press
        if (Time.time - _lastActionTime < ACTION_COOLDOWN) return;
        _lastActionTime = Time.time;

        if (ConversationManager.Instance == null)
        {
            Debug.LogError("[NPCInteractable] ConversationManager not found!");
            return;
        }

        if (_isActive)
            ConversationManager.Instance.EndConversation();
        else
            ConversationManager.Instance.StartConversation(this);
    }

    public void OnBecomeActive()
    {
        _isActive = true;
        if (npcNameLabel != null) npcNameLabel.text = npcName;
        if (actionButtonLabel != null) actionButtonLabel.text = "End";
        if (statusLabel != null) statusLabel.text = "Hold Y to speak";
        if (transcriptLabel != null) transcriptLabel.text = "";
    }

    public void OnBecomeInactive()
    {
        _isActive = false;
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
        _isActive = false;
        if (npcNameLabel != null) npcNameLabel.text = npcName;
        if (actionButtonLabel != null) actionButtonLabel.text = "Start";
        if (statusLabel != null) statusLabel.text = "Press Start to talk";
        if (transcriptLabel != null) transcriptLabel.text = "";
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
