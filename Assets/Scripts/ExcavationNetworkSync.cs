using Unity.Netcode;

[UnityEngine.RequireComponent(typeof(ExcavationDirtPile))]
public class ExcavationNetworkSync : NetworkBehaviour
{
    private ExcavationDirtPile _pile;

    private void Awake()
    {
        _pile = GetComponent<ExcavationDirtPile>();
        _pile.OnChunksExcavated += HandleChunksExcavatedLocally;
    }

    private void OnDestroy()
    {
        if (_pile != null)
            _pile.OnChunksExcavated -= HandleChunksExcavatedLocally;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        NetworkManager.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };

        int count = _pile.ChunkCount;
        for (int i = 0; i < count; i++)
        {
            if (!_pile.IsChunkActive(i))
                ApplyExcavatedClientRpc(i, target);
        }
    }

    private void HandleChunksExcavatedLocally(int[] indices)
    {
        if (!IsSpawned) return;
        foreach (int idx in indices)
            ReportExcavatedServerRpc(idx);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReportExcavatedServerRpc(int index)
    {
        ApplyExcavatedClientRpc(index);
    }

    [ClientRpc]
    private void ApplyExcavatedClientRpc(int index, ClientRpcParams clientRpcParams = default)
    {
        _pile.ForceExcavateChunk(index);
    }
}
