using UnityEngine;
using Unity.Netcode;

public class TeleportCube : MonoBehaviour
{
    public Transform teleportDestination;
    public GameObject xrCardboardRig;
    public MonoBehaviour movementScript;
    public PlaneMenuController menuController;

    private float gazeDistance = 50f;
    private Camera _mainCamera;

    void Update()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        bool controllerInput = Input.GetButtonDown("js2") || Input.GetKeyDown(KeyCode.M);
        Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, gazeDistance))
        {
            if (hit.collider.gameObject.CompareTag("Cube"))
            {
                Debug.Log("reticle on cube");
                if (controllerInput)
                    Teleport();
            }
        }
    }

    void Teleport()
    {
        GameObject player = GetLocalPlayerObject() ?? xrCardboardRig;
        if (player == null)
        {
            Debug.LogWarning("[TeleportCube] No player object found to teleport.");
            return;
        }

        var cc = player.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.position = teleportDestination.position;

        var pm = player.GetComponentInChildren<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        if (menuController != null)
            menuController.NotifyEnteredPlane(player);

        Debug.Log("Teleported inside plane");
    }

    internal static GameObject GetLocalPlayerObject()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsClient || nm.IsHost))
            return nm.LocalClient?.PlayerObject?.gameObject;
        return null;
    }

    internal static void WarpGameObject(GameObject go, Vector3 position)
    {
        var cc = go.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            go.transform.position = position;
            cc.enabled = true;
        }
        else
        {
            go.transform.position = position;
        }
    }
}
