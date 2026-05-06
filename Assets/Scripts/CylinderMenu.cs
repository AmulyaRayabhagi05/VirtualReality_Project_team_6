using UnityEngine;

public class CylinderOpen : MonoBehaviour
{
    public PlaneMenuController menuController;
    public float gazeDistance = 10f;
    private Camera _mainCamera;

    void Update()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, gazeDistance))
        {
            if (hit.collider.gameObject.name == "Cylinder")
            {
                if (Input.GetButtonDown("js2") || Input.GetKeyDown(KeyCode.C))
                {
                    menuController.ShowMenu();
                }
            }
        }
    }
}