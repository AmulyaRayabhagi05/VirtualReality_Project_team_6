using UnityEngine;
using Unity.Netcode;

public class PlayerNetworkSetup : NetworkBehaviour
{
    [Header("Disable for remote players")]
    public Camera playerCamera;
    public AudioListener audioListener;

    [Header("Show for remote players only")]
    [Tooltip("A visible mesh (e.g. capsule) representing this player to others")]
    public GameObject avatarVisual;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameObject lobbyCamera = GameObject.Find("LobbyCamera");
            if (lobbyCamera != null) lobbyCamera.SetActive(false);
        }

        if (playerCamera != null)
            playerCamera.gameObject.SetActive(IsOwner);

        if (audioListener != null)
            audioListener.enabled = IsOwner;

        if (avatarVisual != null)
            avatarVisual.SetActive(!IsOwner);
    }
}
