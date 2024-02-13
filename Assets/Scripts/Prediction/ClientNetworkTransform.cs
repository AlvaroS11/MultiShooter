using Unity.Netcode.Components;
using UnityEngine;


//[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    public PlayerManager playerManager;

    private Vector3 previousPos;

    public AuthorityMode authorityMode = AuthorityMode.Client;

    protected override bool OnIsServerAuthoritative() => authorityMode == AuthorityMode.Server;

    protected override void OnNetworkTransformStateUpdated(ref NetworkTransform.NetworkTransformState oldState, ref NetworkTransform.NetworkTransformState NewState)
    {
        if (!IsServer)
            return;

        playerManager.AntiCheat();
    }
    void Start()
    {
        playerManager = GetComponent<PlayerManager>();
    }

}


public enum AuthorityMode
{
    Server,
    Client
}
