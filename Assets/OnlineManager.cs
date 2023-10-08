using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using static LobbyManager;
using Unity.Services.Lobbies.Models;
using System;
using Unity.Services.Authentication;

public class OnlineManager : NetworkBehaviour
{//CHANGE NAME TO SPAWNER

    public static OnlineManager Instance { get; private set; }

    private enum State
    {
        WaitingToStart,
        CountdownToStart,
        GamePlaying,
        GameOver,
    };

    // [SerializeField] public List<GameObject> playerPrefab;

    //   public Dictionary<string, LobbyManager.PlayerCharacter> playerCharacterMap = new Dictionary<string, LobbyManager.PlayerCharacter>();

    public GameObject playerPrefab;


    private void Start()
    {
        
    }

    public override void OnNetworkSpawn()
    {
     //   Debug.Log("##########");
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }

        Player player = LobbyManager.Instance.GetPlayerOrCreate();

        /*Debug.Log("NEW PLAYER::");
        Debug.Log(player.Data);
        Debug.Log(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value);
        Debug.Log(player.Data[LobbyManager.KEY_PLAYER_TEAM].Value);
        Debug.Log(player.Data[LobbyManager.KEY_PLAYER_NAME].Value);
        Debug.Log(LobbyManager.Instance.GetTeam(player.Id));
        */


        PlayerCharacter playerCharacter = Enum.Parse<PlayerCharacter>(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value);

        SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId, playerCharacter);

     //   Debug.Log(NetworkManager.Singleton.LocalClientId);
    }

    private void OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform playerTransform = Instantiate(playerPrefab.transform);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }

    private void OnLoadEventCompleted2()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform playerTransform = Instantiate(playerPrefab.transform);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }


    //     Transform playerTransform = Instantiate(playerPrefab, transform.position, transform.rotation);
    //   playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.LocalClientId, true);

    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRpc(ulong clientId, LobbyManager.PlayerCharacter playerCharacter)
    {
     //   ulong clientId = NetworkManager.Singleton.LocalClientId;
        Debug.Log("SPAWNING PLAYER!!");
        Debug.Log(clientId);

        GameObject prefab = LobbyAssets.Instance.GetPrefab(playerCharacter);
        GameObject newPlayer = (GameObject)Instantiate(prefab);


        Player lobbyPlayer = LobbyManager.Instance.GetPlayerOrCreate();

        newPlayer.GetComponent<PlayerManager>().team = int.Parse(lobbyPlayer.Data[LobbyManager.KEY_PLAYER_TEAM].Value);

        Debug.Log(lobbyPlayer.Data[LobbyManager.KEY_PLAYER_NAME].Value);

        Debug.Log(lobbyPlayer.Data[LobbyManager.KEY_PLAYER_TEAM].Value);

        newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        newPlayer.SetActive(true);
    }

    [ClientRpc]
    public void SetTeamsClientRpc()
    {

    }


    private void OnClientDisconnectCallback(ulong clientId)
    {
        //autoTestGamePausedState = true;
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
          //  NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SpawnPlayerServerRpc;

        }
    }

}
