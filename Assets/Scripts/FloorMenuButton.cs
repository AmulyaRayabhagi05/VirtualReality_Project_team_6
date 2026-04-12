using UnityEngine;

public class FloorMenuButton : MonoBehaviour
{
    public FloorsMenuController floorMenuController;

    private void Update()
    {
        if (Input.GetButtonDown("js5") || Input.GetKeyDown(KeyCode.E))
        {
            floorMenuController.OpenMenu();
        }
    }
}