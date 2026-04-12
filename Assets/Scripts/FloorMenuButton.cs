using UnityEngine;

public class FloorMenuButton : MonoBehaviour
{
    public FloorsMenuController floorMenuController;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.E))
        {
            floorMenuController.OpenMenu();
        }
    }
}