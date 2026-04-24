using UnityEngine;

public class FloorMenuButton : MonoBehaviour
{
    public FloorsMenuController floorMenuController;

    private void Start()
    {
        if (floorMenuController == null)
            floorMenuController = Object.FindAnyObjectByType<FloorsMenuController>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (floorMenuController == null) return;

        if (Input.GetButtonDown("js5") || Input.GetKeyDown(KeyCode.E))
        {
            floorMenuController.OpenMenu();
        }
    }
}