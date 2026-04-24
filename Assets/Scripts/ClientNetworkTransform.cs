using Unity.Netcode.Components;

// Drop this on the player prefab instead of NetworkTransform.
// Gives each client authority over their own player's position.
[UnityEngine.DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative() => false;
}
