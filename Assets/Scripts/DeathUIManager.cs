using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the full-screen death UI.
///
/// CANVAS SETUP:
///   1. GameObject → UI → Canvas
///        - Render Mode: Screen Space - Overlay
///        - Canvas Scaler: Scale With Screen Size
///   2. Inside Canvas, add a Panel (Image component, set color to dark semi-transparent)
///        - Anchor: stretch to fill entire canvas (alt+shift click the anchor box → stretch all)
///   3. Inside Panel add:
///        - TextMeshProUGUI  → "YOU DIED"         (titleText)
///        - TextMeshProUGUI  → death message       (messageText)
///        - Button           → "Try Again?"        (restartButton)
///   4. Attach THIS script to the Canvas GameObject
///   5. Assign all fields in Inspector
/// </summary>
public class DeathUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject deathPanel;       // the full-screen panel inside the canvas
    public TextMeshProUGUI titleText;         // big "YOU DIED" text
    public TextMeshProUGUI messageText;       // flavour death message
    public Button restartButton;

    [Header("Text Content")]
    public string titleMessage = "YOU DIED";
    public string deathMessage = "You crashed!\nWant to start over?";
    public string restartLabel = "Try Again";

    [Header("Restart")]
    [Tooltip("Leave blank to just re-enable flying in the same scene")]
    public string restartSceneName = "";

    public CollisionDeathHandler deathHandler;
    private CanvasGroup deathPanelGroup;
    private bool isRestarting = false;

    void Start()
    {
        deathHandler = FindObjectOfType<CollisionDeathHandler>();

        // Add a CanvasGroup to the panel and hide it that way
        deathPanelGroup = deathPanel.GetComponent<CanvasGroup>();
        if (deathPanelGroup == null)
            deathPanelGroup = deathPanel.AddComponent<CanvasGroup>();

        // Hide without deactivating — keeps button listeners alive
        HideDeathScreen();

        if (restartButton)
        {
            var btnText = restartButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText) btnText.text = restartLabel;
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (restartButton)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
            Debug.Log("Button listener added successfully"); // add this
        }
        else
        {
            Debug.LogError("restartButton is NULL in DeathUIManager!"); // add this
        }


    }

    public void ShowDeathScreen()
    {
        if (titleText) titleText.text = titleMessage;
        if (messageText) messageText.text = deathMessage;

        deathPanelGroup.alpha = 1f;
        deathPanelGroup.interactable = true;
        deathPanelGroup.blocksRaycasts = true;

        // Force EventSystem to reset so button is clickable
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    public void HideDeathScreen()
    {
        // Make invisible but keep active
        deathPanelGroup.alpha = 0f;
        deathPanelGroup.interactable = false;
        deathPanelGroup.blocksRaycasts = false;
    }


    public void OnRestartClicked()
    {
        if (isRestarting) return;  // prevent multiple calls
        isRestarting = true;

        Debug.Log("Restart button clicked!");
        HideDeathScreen();

        if (!string.IsNullOrEmpty(restartSceneName))
        {
            SceneManager.LoadScene(restartSceneName);
        }
        else
        {
            if (deathHandler == null)
                Debug.LogError("deathHandler is NULL!");
            else
                deathHandler.Restart();
        }

        isRestarting = false;
    }

}