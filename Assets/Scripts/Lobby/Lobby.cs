using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    // Start is called before the first frame update

    private Lobby hostLobby;

    private float hearBeatSeconds;
    private async void Start()
    {
        await UnityServices.InitializeAsync();


        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void Update()
    {
        LobbyHearbeat();
    }

    private async void LobbyHearbeat()
    {
        if (hostLobby == null) return;
        hearBeatSeconds -= Time.deltaTime;
        if(hearBeatSeconds <= 0f)
        {
            float heartbeatTimer = 15;
            hearBeatSeconds = heartbeatTimer;

            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
        }
    }

    private async void CreateLobby()
    {
        try
        {
            string lobbyName = "dd";
            int maxPlayers = 4; // Refer to ConnectionAproval
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);

            hostLobby = lobby;

            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers);
        }catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    private async void ListLobbies()
    {
        QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
        {
            Count = 25,
            Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                //25 Lobbies max, where available slots are gt(greater than) 0

            },
            Order = new List<QueryOrder>
            {
                new QueryOrder(false, QueryOrder.FieldOptions.Created)
            }
        };

        QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

        Debug.Log("Lobbies found: " + queryResponse.Results.Count);


        foreach (Lobby lobby in queryResponse.Results)
        {
            Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
        }
    }

    private void JoinLobby()
    {
       // Lobbies.Instance.JoinLobbyByIdAsync();
    }
}
