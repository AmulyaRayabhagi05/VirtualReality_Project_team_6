using UnityEngine;
using Unity.Netcode;

// Add this alongside NPCInteractable on the same GameObject, then add a NetworkObject too.
// NPCInteractable routes all state changes through here so every client stays in sync.
[RequireComponent(typeof(NPCInteractable))]
public class NPCNetworkSync : NetworkBehaviour
{
    private NPCInteractable _npc;

    // Tracks active state so late-joining clients see the NPC correctly
    private NetworkVariable<bool> _isInConversation = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake() => _npc = GetComponent<NPCInteractable>();

    public override void OnNetworkSpawn()
    {
        if (_isInConversation.Value)
            _npc.OnBecomeActiveLocal();
    }

    public void BroadcastBecomeActive()
    {
        if (IsSpawned) BecomeActiveServerRpc();
    }

    public void BroadcastBecomeInactive()
    {
        if (IsSpawned) BecomeInactiveServerRpc();
    }

    public void BroadcastStatus(string msg)
    {
        if (IsSpawned) SetStatusServerRpc(msg);
    }

    public void BroadcastTranscript(string msg)
    {
        if (IsSpawned) SetTranscriptServerRpc(msg);
    }

    [ServerRpc(RequireOwnership = false)]
    private void BecomeActiveServerRpc()
    {
        _isInConversation.Value = true;
        BecomeActiveClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void BecomeInactiveServerRpc()
    {
        _isInConversation.Value = false;
        BecomeInactiveClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetStatusServerRpc(string msg) => SetStatusClientRpc(msg);

    [ServerRpc(RequireOwnership = false)]
    private void SetTranscriptServerRpc(string msg) => SetTranscriptClientRpc(msg);

    [ClientRpc]
    private void BecomeActiveClientRpc() => _npc.OnBecomeActiveLocal();

    [ClientRpc]
    private void BecomeInactiveClientRpc() => _npc.OnBecomeInactiveLocal();

    [ClientRpc]
    private void SetStatusClientRpc(string msg) => _npc.SetStatusLocal(msg);

    [ClientRpc]
    private void SetTranscriptClientRpc(string msg) => _npc.SetTranscriptLocal(msg);
}
