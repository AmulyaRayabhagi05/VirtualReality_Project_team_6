using UnityEngine;
using Unity.Netcode;
[RequireComponent(typeof(MapManager))]
public class MapNetworkSync : NetworkBehaviour
{
    private MapManager _manager;

    // -1 = hidden, 0 = dinosaur, 1 = plane, 2 = pyramid
    private NetworkVariable<int> _selectedPanel = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake() => _manager = GetComponent<MapManager>();

    public override void OnNetworkSpawn()
    {
        _selectedPanel.OnValueChanged += OnSelectedPanelChanged;
        OnSelectedPanelChanged(-2, _selectedPanel.Value);
    }

    public override void OnNetworkDespawn()
    {
        _selectedPanel.OnValueChanged -= OnSelectedPanelChanged;
    }

    public void RequestShowPanel(int index)
    {
        if (IsSpawned) RequestPanelServerRpc(index);
        else _manager.ApplyPanelSelection(index);
    }

    public void RequestHidePanel()
    {
        if (IsSpawned) RequestPanelServerRpc(-1);
        else _manager.ApplyPanelSelection(-1);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPanelServerRpc(int index) => _selectedPanel.Value = index;

    private void OnSelectedPanelChanged(int previous, int current) => _manager.ApplyPanelSelection(current);
}
