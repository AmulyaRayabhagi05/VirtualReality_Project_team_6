using UnityEngine;
public class TeleportCube : MonoBehaviour
{
    public Transform teleportDestination;
    public GameObject character;
    public GameObject xrCardboardRig;
    public MonoBehaviour movementScript;
    public PlaneMenuController menuController;

    private float gazeDistance = 50f;

    void Update()
    {
        bool controllerInput = Input.GetButtonDown("js2")  || Input.GetKeyDown(KeyCode.M);
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, gazeDistance))
        {
            if (hit.collider.gameObject.CompareTag("Cube"))
            {
                Debug.Log("reticle on cube");
                if (controllerInput)
                {
                    Teleport();
                }
            }
        }
    }

    void Teleport()
    {
        xrCardboardRig.transform.position = teleportDestination.position;

        if (movementScript != null){
            movementScript.enabled = false;
	}
        
        if (menuController != null){
            menuController.SetMovementScript(movementScript);
	}

        Debug.Log("Teleported inside plane");
    }
}