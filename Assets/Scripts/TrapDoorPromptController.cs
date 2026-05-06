using UnityEngine;
using UnityEngine.UI;

public class TrapDoorPromptController : MonoBehaviour
{
    [SerializeField] private Canvas promptCanvas;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, 1.5f);
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private bool debugPrompt;
    [SerializeField] private PlayerMovement playerMovement;

    public float stickThreshold = 0.5f;
    public float navigationCooldown = 0.3f;

    private int _selectedIndex = 0;
    private const int OPTION_COUNT = 2;
    private bool _stickNeutral = true;
    private float _cooldownTimer = 0f;

    private static readonly Color SELECTED_COLOR   = new Color(1f, 0.85f, 0f);
    private static readonly Color UNSELECTED_COLOR = Color.white;

    private Camera _runtimeCamera;
    private string destinationScene = "Underground";

    public bool IsOpen
    {
        get
        {
            return promptCanvas != null && promptCanvas.gameObject.activeSelf;
        }
    }

    private void Update()
    {
        if (!IsOpen) return;

        _cooldownTimer -= Time.unscaledDeltaTime;
        HandleNavigation();
        HandleConfirm();
    }

    private void Awake()
    {
        _runtimeCamera = Camera.main;

        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(UseTrapDoor);
            yesButton.onClick.AddListener(UseTrapDoor);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(ClosePrompt);
            noButton.onClick.AddListener(ClosePrompt);
        }

        if (promptCanvas != null)
        {
            promptCanvas.gameObject.SetActive(false);
        }
    }

    public void OpenPrompt()
    {
        if (promptCanvas == null)
        {
            LogDebug("OpenPrompt ignored because promptCanvas is missing.");
            return;
        }

        PositionPromptInFrontOfPlayer();
        promptCanvas.gameObject.SetActive(true);

        _selectedIndex = 0;
        _stickNeutral = true;
        _cooldownTimer = 0f;
        UpdateHighlight();
        if (playerMovement != null) playerMovement.SetMovementEnabled(false);
        Time.timeScale = 0f;
        LogDebug("Prompt opened.");
    }

    public void ClosePrompt()
    {
        if (promptCanvas == null)
        {
            return;
        }

        _stickNeutral = true;
        _cooldownTimer = 0f;
        promptCanvas.gameObject.SetActive(false);
        if (playerMovement != null) playerMovement.SetMovementEnabled(true);
        Time.timeScale = 1f;
        LogDebug("Prompt closed.");
    }

    public void UseTrapDoor()
    {
        LogDebug($"Loading destination scene '{destinationScene}'.");
        ClosePrompt();
        NetworkSceneLoader.Load(destinationScene);
    }

    private void HandleNavigation()
    {
        float axis = Input.GetAxis("Vertical");

        if (Mathf.Abs(axis) < stickThreshold)
        {
            _stickNeutral = true;
            return;
        }

        if (!_stickNeutral || _cooldownTimer > 0f) return;

        if (axis > stickThreshold)
            _selectedIndex = (_selectedIndex - 1 + OPTION_COUNT) % OPTION_COUNT;
        else
            _selectedIndex = (_selectedIndex + 1) % OPTION_COUNT;

        _stickNeutral = false;
        _cooldownTimer = navigationCooldown;
        UpdateHighlight();
    }

    private void HandleConfirm()
    {
        if (!Input.GetKeyDown(KeyCode.JoystickButton2)) return;

        switch (_selectedIndex)
        {
            case 0: UseTrapDoor(); break;
            case 1: ClosePrompt(); break;
        }
    }

    private void UpdateHighlight()
    {
        SetButtonColor(yesButton, _selectedIndex == 0 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetButtonColor(noButton,  _selectedIndex == 1 ? SELECTED_COLOR : UNSELECTED_COLOR);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var c = btn.colors;
        c.normalColor = color;
        btn.colors = c;
    }

    private void PositionPromptInFrontOfPlayer()
    {
        if (_runtimeCamera == null || !_runtimeCamera.gameObject.activeInHierarchy)
        {
            _runtimeCamera = Camera.main;
        }

        if (_runtimeCamera == null)
        {
            return;
        }

        Transform cam = _runtimeCamera.transform;
        promptCanvas.transform.position = cam.position + cam.TransformDirection(spawnOffset);

        if (!facePlayer)
        {
            return;
        }

        Vector3 lookDir = promptCanvas.transform.position - cam.position;
        lookDir.y = 0f;

        if (lookDir != Vector3.zero)
        {
            promptCanvas.transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    private void LogDebug(string message)
    {
        if (!debugPrompt)
        {
            return;
        }

        Debug.Log($"[TrapDoorPromptController] {message}", this);
    }
}
