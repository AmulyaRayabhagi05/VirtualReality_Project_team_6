using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private bool js0WasPressed = false;
    private FlyController fly;

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

        if (backButton)
        {
            backButton.onClick.AddListener(OnBackClicked);
            var backBtnText = backButton.GetComponentInChildren<TextMeshProUGUI>();
            if (backBtnText) backBtnText.text = "Back to Museum";
            Debug.Log("Back button listener added");
        }else{
            Debug.LogWarning("backButton is NULL");
        }

        if (restartButton)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
            Debug.Log("Button listener added");
        }else{
            Debug.LogError("restartButton is NULL");
        }


    }

    void Update()
    {
        if (deathPanelGroup.alpha <= 0f) {
		return;
	}

        HandleGazeAndController();
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
    }

    public void HideDeathScreen()
    {
        deathPanelGroup.alpha = 0f;
        deathPanelGroup.interactable = false;
        deathPanelGroup.blocksRaycasts = false;
    }

    void HandleGazeAndController()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Screen.width / 2f, Screen.height / 2f)
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        GameObject gazedButton = null;

        foreach (var r in results)
        {
            Button btn = r.gameObject.GetComponent<Button>();
            if (btn != null)
            {
                gazedButton = r.gameObject;
                break;
            }
        }

        if (gazedButton != null)
        {
            Debug.Log("Looking at:" + gazedButton.name);

            if (Input.GetButtonDown("js0"))
            {
                Debug.Log("js0 pressed on:" + gazedButton.name);

                Button btn = gazedButton.GetComponent<Button>();
                if (btn != null){
                    btn.onClick.Invoke();
		}
            }
        }
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