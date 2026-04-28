using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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
