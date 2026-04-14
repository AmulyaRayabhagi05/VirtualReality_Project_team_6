using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathUIManager : MonoBehaviour
{
    [Header("References")]
    public GameObject deathPanel;       
    public TextMeshProUGUI titleText;         
    public TextMeshProUGUI messageText;
    public Button restartButton;

    [Header("Text")]
    public string titleMessage = "YOU DIED";
    public string deathMessage = "You crashed";
    public string restartLabel = "Try Again";

    public Button backButton;
    public string backSceneName = "SecondFloor";

    public CollisionDeathHandler deathHandler;
    private CanvasGroup deathPanelGroup;
    private bool isRestarting = false;
    private FlyController fly;

    public float stickThreshold = 0.5f;
    public float navigationCooldown = 0.3f;

    private int _selectedIndex = 0;
    private const int OPTION_COUNT = 2;
    private bool _stickNeutral = true;
    private float _cooldownTimer = 0f;

    private static readonly Color SELECTED_COLOR = new Color(1f, 0.85f, 0f);
    private static readonly Color UNSELECTED_COLOR = Color.white;

    void Start()
    {
        deathHandler = FindObjectOfType<CollisionDeathHandler>();

        fly = FindFirstObjectByType<FlyController>();

        deathPanelGroup = deathPanel.GetComponent<CanvasGroup>();
        if (deathPanelGroup == null){
            deathPanelGroup = deathPanel.AddComponent<CanvasGroup>();
	}
        HideDeathScreen();

        if (restartButton)
        {
            var btnText = restartButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText) btnText.text = restartLabel;
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        else
        {
            Debug.LogError("restartButton is NULL");
        }

        if (backButton)
        {
            backButton.onClick.AddListener(OnBackClicked);
            var backBtnText = backButton.GetComponentInChildren<TextMeshProUGUI>();
            if (backBtnText) backBtnText.text = "Back to Museum";
        }
        else
        {
            Debug.LogWarning("backButton is NULL");
        }
    }

    void Update()
    {
        if (deathPanelGroup.alpha <= 0f)
        {
            return;
        }

        _cooldownTimer -= Time.unscaledDeltaTime;
        HandleNavigation();
        HandleConfirm();
    }

    public void OnBackClicked()
    {
        Debug.Log("Back button clicked");
        HideDeathScreen();

        if (!string.IsNullOrEmpty(backSceneName))
        {
            SceneManager.LoadScene(backSceneName);
        }else{
            Debug.LogWarning("backSceneName is empty");
        }
    }

    public void ShowDeathScreen()
    {
        if (titleText){
		titleText.text = titleMessage;
        }
	
	if (messageText){ 
		messageText.text = deathMessage;
	}

	fly?.DisableInput();
        LockToCamera();
        deathPanelGroup.alpha = 1f;
        deathPanelGroup.interactable = true;
        deathPanelGroup.blocksRaycasts = true;

        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        _selectedIndex = 0;
        _stickNeutral = true;
        _cooldownTimer = 0f;
        UpdateHighlight();
    }

    public void HideDeathScreen()
    {
        _stickNeutral = true;
        _cooldownTimer = 0f;
        deathPanelGroup.alpha = 0f;
        deathPanelGroup.interactable = false;
        deathPanelGroup.blocksRaycasts = false;
    }

    private void HandleNavigation()
    {
        float axis = Input.GetAxis("Vertical");

        if (Mathf.Abs(axis) < stickThreshold)
        {
            _stickNeutral = true;
            return;
        }

        if (!_stickNeutral || _cooldownTimer > 0f)
        {
            return;
        }

        if (axis > stickThreshold)
        {
            _selectedIndex = (_selectedIndex - 1 + OPTION_COUNT) % OPTION_COUNT;
        }
        else
        {
            _selectedIndex = (_selectedIndex + 1) % OPTION_COUNT;
        }

        _stickNeutral = false;
        _cooldownTimer = navigationCooldown;
        UpdateHighlight();
    }

    private void HandleConfirm()
    {
        if (!Input.GetKeyDown(KeyCode.JoystickButton2))
        {
            return;
        }

        switch (_selectedIndex)
        {
            case 0:
                OnRestartClicked();
                break;
            case 1:
                OnBackClicked();
                break;
        }
    }

    private void UpdateHighlight()
    {
        SetButtonColor(restartButton, _selectedIndex == 0 ? SELECTED_COLOR : UNSELECTED_COLOR);
        SetButtonColor(backButton, _selectedIndex == 1 ? SELECTED_COLOR : UNSELECTED_COLOR);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null)
        {
            return;
        }

        var c = btn.colors;
        c.normalColor = color;
        btn.colors = c;
    }

    void LockToCamera()
    {
        Transform cam = Camera.main.transform;
        transform.position = cam.position + cam.forward * 2f;
        transform.rotation = Quaternion.LookRotation(
            transform.position - cam.position
        );
    }

    public void OnRestartClicked()
    {
        if (isRestarting) return;
        isRestarting = true;

        Debug.Log("Restart button clicked");
        HideDeathScreen();

        deathHandler.Restart();
        

        fly?.EnableInput();
        isRestarting = false;
    }

}