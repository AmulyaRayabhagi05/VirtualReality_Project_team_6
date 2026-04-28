using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Place this on a GameObject with a NetworkObject in every scene.
// When any player changes floor, the server migrates all clients together.
public class NetworkSceneLoader : NetworkBehaviour
{
    public static NetworkSceneLoader Instance { get; private set; }

    public override void OnNetworkSpawn()
    {
        Instance = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
    }

    // Call from anywhere. In multiplayer all clients change scene together.
    // Falls back to local load when not networked (single-player / editor).
    public static void Load(string sceneName)
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && Instance != null && Instance.IsSpawned)
        {
            Instance.RequestLoadServerRpc(sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestLoadServerRpc(string sceneName)
    {
        NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
