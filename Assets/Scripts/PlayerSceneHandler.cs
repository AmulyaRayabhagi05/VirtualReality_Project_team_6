using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PlayerSceneHandler : NetworkBehaviour
{
    public static bool RestoreMovementOnLoad { get; set; }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnNetworkDespawn()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsOwner) return;
        if (!RestoreMovementOnLoad) return;
        RestoreMovementOnLoad = false;

        var cc = GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;

        var planeMenu = FindAnyObjectByType<PlaneMenuController>(FindObjectsInactive.Include);
        if (planeMenu != null && planeMenu.outsideDestination != null)
            transform.position = planeMenu.outsideDestination.position;

        if (cc != null) cc.enabled = true;

        var pm = GetComponentInChildren<PlayerMovement>();
        if (pm != null) pm.enabled = true;
    }
}
