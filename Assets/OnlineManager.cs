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
    public string PlayerLobbyId;
    public string PlayerName;
    public string PlayerTeam;
    public string playerCharacterr;


    private void Awake() {
        Instance = this;
    }

    private void Start()
    {
    }

    public override void OnNetworkSpawn()
    {
        //   Debug.Log("##########");
        PlayerLobbyId = AuthenticationService.Instance.PlayerId;

        //PlayerName = 

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }

        /*  Player player = LobbyManager.Instance.GetPlayerById(PlayerLobbyId);

          PlayerCharacter playerCharacter = Enum.Parse<PlayerCharacter>(player.Data[KEY_PLAYER_CHARACTER].Value);

          Debug.Log("ÑÑÑÑ");
          Debug.Log(player.Data[KEY_PLAYER_CHARACTER].Value);
          Debug.Log(playerCharacter);

          LobbyManager.Instance.logPlayer();

          SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId, playerCharacter);
        */
        SetUpPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    public void SetUpLocalVariables(string team, string name)
    {
        Debug.Log("SETUP LOCAL!");
        //Localmente funciona aquí
        PlayerTeam = team;
        PlayerName = name;
    }


    //Se tiene que llamar en el OnNetworkSpawn
    [ServerRpc(RequireOwnership = false)]
    public void SetUpPlayerServerRpc(ulong clientId)
    {
        try
        {
            Debug.Log("SETUP SERVER!");
            Player player = LobbyManager.Instance.GetPlayerById(PlayerLobbyId);
            PlayerCharacter playerCharacter = Enum.Parse<PlayerCharacter>(player.Data[KEY_PLAYER_CHARACTER].Value);
            GameObject prefab = LobbyAssets.Instance.GetPrefab(playerCharacter);
            GameObject newPlayer = (GameObject)Instantiate(prefab);

            newPlayer.GetComponent<PlayerManager>().PlayerTeam = PlayerTeam;
            newPlayer.GetComponent<PlayerManager>().name = PlayerName;


            newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

            Debug.Log("INSTANTIATED");
        }
        catch(Exception e)
        {
            Debug.Log(e);
        }

        /* Player player = LobbyManager.Instance.GetPlayerById(PlayerLobbyId);
         PlayerCharacter playerCharacter = Enum.Parse<PlayerCharacter>(player.Data[KEY_PLAYER_CHARACTER].Value);

         GameObject prefab = LobbyAssets.Instance.GetPrefab(playerCharacter);

         GameObject newPlayer = (GameObject)Instantiate(prefab);

         prefab.GetComponent<PlayerManager>().PlayerTeam = player.Data[KEY_PLAYER_TEAM].Value;
         Debug.Log(player.Data[KEY_PLAYER_TEAM].Value);

         newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        */
    }

    [ClientRpc]
    private void SetUpPlayerClientRpc()
    {

    }

  /*  private void OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform playerTransform = Instantiate(playerPrefab.transform);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }

    */


    //     Transform playerTransform = Instantiate(playerPrefab, transform.position, transform.rotation);
    //   playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.LocalClientId, true);

   
    
    
   /* [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRpc(ulong clientId, LobbyManager.PlayerCharacter playerCharacter)
    {
     //   ulong clientId = NetworkManager.Singleton.LocalClientId;
     //   Debug.Log("SPAWNING PLAYER!!");
       // Debug.Log(clientId);

        GameObject prefab = LobbyAssets.Instance.GetPrefab(playerCharacter);
        GameObject newPlayer = (GameObject)Instantiate(prefab);


        Player lobbyPlayer = LobbyManager.Instance.GetPlayerById(PlayerLobbyId);




        newPlayer.GetComponent<PlayerManager>().team = int.Parse(lobbyPlayer.Data[LobbyManager.KEY_PLAYER_TEAM].Value);

        Debug.Log(lobbyPlayer.Data[LobbyManager.KEY_PLAYER_NAME].Value);

        Debug.Log(lobbyPlayer.Data[LobbyManager.KEY_PLAYER_TEAM].Value);

        newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        newPlayer.SetActive(true);


        LobbyManager.Instance.logPlayer();

    }
   */

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
        //    NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
          //  NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SpawnPlayerServerRpc;

        }
    }

}
