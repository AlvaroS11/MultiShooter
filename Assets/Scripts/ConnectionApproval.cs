using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ConnectionApproval : MonoBehaviour
{
    public int MaxPlayers;

    void Start()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback = Check; //cuando termina el callback se ejecuta el check
    }


    [ServerRpc]
    private void Check(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;

        response.CreatePlayerObject = true;
        response.PlayerPrefabHash = null;


        if(NetworkManager.Singleton.ConnectedClients.Count >= MaxPlayers)
        {
            response.Approved = false;
            response.Reason = "Server full";
        }

        response.Pending = false;
        //response.Reason

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
