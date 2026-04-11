using UnityEngine;

public class CylinderOpen : MonoBehaviour
{
    public PlaneMenuController menuController;
    public float gazeDistance = 10f;

    void Update()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, gazeDistance))
        {
            if (hit.collider.gameObject.name == "Cylinder")
            {
                if (Input.GetButtonDown("js0"))
                {
                    Debug.Log("Cylinder hit and js0 pressed so open menu");
                    menuController.ShowMenu();
                }
            }
        }
    }
}