using UnityEngine;

public class VRPauseButton : MonoBehaviour
{
    public PauseMenuController pauseMenuController;

    private void Update()
    {
        if (Input.GetButtonDown("js7") || Input.GetKeyDown(KeyCode.Q))
        {
            pauseMenuController.PauseGame();
        }
    }
}