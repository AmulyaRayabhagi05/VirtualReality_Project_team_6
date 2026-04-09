using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public Canvas menuCanvas;
    public Vector3 spawnOffset = new Vector3(0f, 0f, 1.5f);
    public bool facePlayer = true;
    public Button resumeButton;
    public Button settingsButton;
    public float stickThreshold = 0.5f;
    public float navigationCooldown = 0.3f;
    public CharacterMovement characterMovement;

    private int _selectedIndex = 0;
    private const int OPTION_COUNT = 2;

    private bool _stickNeutral = true;
    private float _cooldownTimer = 0f;

    private static readonly Color SELECTED_COLOR   = new Color(1f, 0.85f, 0f);
    private static readonly Color UNSELECTED_COLOR = Color.white;

    private Camera _vrCamera;

    private void Awake()
    {
        _vrCamera = Camera.main;
        menuCanvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!pausePanel.activeSelf) return;

        _cooldownTimer -= Time.unscaledDeltaTime;
        HandleNavigation();
        HandleConfirm();
    }

    public void PauseGame()
    {
        if (characterMovement != null) characterMovement.enabled = false;
        PositionMenuInFrontOfPlayer();
        menuCanvas.gameObject.SetActive(true);
        ShowPausePanel();
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (characterMovement != null) characterMovement.enabled = true;
        _stickNeutral = true;
        _cooldownTimer = 0f;
        menuCanvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OpenSettings()
    {
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void BackToPauseMenu()
    {
        settingsPanel.SetActive(false);
        ShowPausePanel();
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

        // Up (positive) = previous option, Down (negative) = next option
        if (axis > stickThreshold)
            _selectedIndex = (_selectedIndex - 1 + OPTION_COUNT) % OPTION_COUNT;
        else
            _selectedIndex = (_selectedIndex + 1) % OPTION_COUNT;

        _stickNeutral = false;
        _cooldownTimer = navigationCooldown;
        UpdatePauseHighlight();
    }

    private void HandleConfirm()
    {
        if (!Input.GetButtonDown("js5")) return;

        switch (_selectedIndex)
        {
            case 0: ResumeGame();   break;
            case 1: OpenSettings(); break;
        }
    }

    private void UpdatePauseHighlight()
    {
        SetButtonColor(resumeButton,   _selectedIndex == 0 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetButtonColor(settingsButton, _selectedIndex == 1 ? SELECTED_COLOR : UNSELECTED_COLOR);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var c = btn.colors;
        c.normalColor = color;
        btn.colors = c;
    }

    private void ShowPausePanel()
    {
        _selectedIndex = 0;
        pausePanel.SetActive(true);
        settingsPanel.SetActive(false);
        UpdatePauseHighlight();
    }

    private void PositionMenuInFrontOfPlayer()
    {
        if (_vrCamera == null) return;

        Transform cam = _vrCamera.transform;
        menuCanvas.transform.position = cam.position + cam.TransformDirection(spawnOffset);

        if (facePlayer)
        {
            Vector3 lookDir = menuCanvas.transform.position - cam.position;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
                menuCanvas.transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}