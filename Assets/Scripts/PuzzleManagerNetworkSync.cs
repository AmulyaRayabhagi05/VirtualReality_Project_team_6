using UnityEngine;
using Unity.Netcode;

// Add this alongside PuzzleAssemblyManager on the same GameObject, then add a NetworkObject too.
// PuzzleAssemblyManager stays a plain MonoBehaviour so its input handling is never affected by
// network state. This script handles only the sync layer.
[RequireComponent(typeof(PuzzleAssemblyManager))]
public class PuzzleManagerNetworkSync : NetworkBehaviour
{
    private PuzzleAssemblyManager _manager;

    private void Awake()
    {
        _manager = GetComponent<PuzzleAssemblyManager>();
        _manager.OnPiecePlacedLocally += HandlePiecePlacedLocally;
    }

    private void OnDestroy()
    {
        if (_manager != null)
            _manager.OnPiecePlacedLocally -= HandlePiecePlacedLocally;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        // When a new client connects after pieces are already placed, push the current state to them.
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

        int count = _manager.GetPieceCount();
        for (int i = 0; i < count; i++)
        {
            if (_manager.IsPiecePlaced(i))
                BroadcastPlacedClientRpc(i, target);
        }
    }

    private void HandlePiecePlacedLocally(int pieceIndex)
    {
        if (!IsSpawned) return;
        ReportPlacedServerRpc(pieceIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReportPlacedServerRpc(int pieceIndex)
    {
        BroadcastPlacedClientRpc(pieceIndex);
    }

    [ClientRpc]
    private void BroadcastPlacedClientRpc(int pieceIndex, ClientRpcParams clientRpcParams = default)
    {
        // ForcePlacePiece is a no-op if the piece is already placed (handles the placing client)
        _manager.ForcePlacePiece(pieceIndex);
    }
}
