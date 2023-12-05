using Unity.Netcode.Components;
using UnityEngine;


//[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    public AuthorityMode authorityMode = AuthorityMode.Client;

    protected override bool OnIsServerAuthoritative() => authorityMode == AuthorityMode.Server;
}


public enum AuthorityMode
{
    Server,
    Client
}
