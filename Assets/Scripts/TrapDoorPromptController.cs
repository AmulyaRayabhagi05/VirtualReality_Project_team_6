using UnityEngine;
using UnityEngine.UI;

public class TrapDoorPromptController : MonoBehaviour
{
    [SerializeField] private Canvas promptCanvas;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private string destinationScene = "SecondFloor";
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, 1.5f);
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private bool debugPrompt;

    private Camera _runtimeCamera;

    public bool IsOpen
    {
        get
        {
            return promptCanvas != null && promptCanvas.gameObject.activeSelf;
        }
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
        LogDebug("Prompt opened.");
    }

    public void ClosePrompt()
    {
        if (promptCanvas == null)
        {
            return;
        }

        promptCanvas.gameObject.SetActive(false);
        LogDebug("Prompt closed.");
    }

    public void UseTrapDoor()
    {
        LogDebug($"Loading destination scene '{destinationScene}'.");
        ClosePrompt();
        NetworkSceneLoader.Load(destinationScene);
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
