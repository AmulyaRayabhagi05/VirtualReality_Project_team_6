using UnityEngine;

public class VRPauseButton : MonoBehaviour
{
    public PauseMenuController pauseMenuController;

    private void Start()
    {
        if (pauseMenuController == null)
            pauseMenuController = Object.FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (pauseMenuController == null)
            pauseMenuController = Object.FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include);

        if (pauseMenuController == null) return;

        if (Input.GetButtonDown("js7") || Input.GetKeyDown(KeyCode.Q))
        {
            pauseMenuController.PauseGame();
        }
    }
}