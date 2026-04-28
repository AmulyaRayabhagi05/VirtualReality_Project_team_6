using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(DinoTransitionController))]
public class DinoNetworkSync : NetworkBehaviour
{
    private DinoTransitionController _controller;

    private NetworkVariable<int> _phase = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake() => _controller = GetComponent<DinoTransitionController>();

    public override void OnNetworkSpawn()
    {
        _controller.ApplyPhaseImmediate(_phase.Value);
    }

    public void OnButtonPressedNetworked()
    {
        if (IsSpawned)
            ButtonPressedServerRpc();
        else
            _controller.OnButtonPressed();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ButtonPressedServerRpc()
    {
        _phase.Value = _phase.Value == 0 ? 1 : 0;
        ButtonPressedClientRpc();
    }

    [ClientRpc]
    private void ButtonPressedClientRpc()
    {
        _controller.OnButtonPressed();
    }
}
