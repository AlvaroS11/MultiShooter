using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using static LobbyManager;
using Unity.Services.Lobbies.Models;
using System;
using Unity.Services.Authentication;
using Unity.Networking.Transport;
using System.Reflection;
using UnityEngine.TextCore.Text;

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

    private NetworkVariable<State> state = new NetworkVariable<State>(State.WaitingToStart);


    // [SerializeField] public List<GameObject> playerPrefab;

    //   public Dictionary<string, LobbyManager.PlayerCharacter> playerCharacterMap = new Dictionary<string, LobbyManager.PlayerCharacter>();

    public GameObject playerPrefab;
    public string PlayerLobbyId;
    public string PlayerName;
    public string PlayerTeam;
    public string playerCharacterr;

    public bool waiting = true;


    private Dictionary<ulong, bool> playerReadyDictionary;

    public Dictionary<string, int> playerTeamDictionary;

    public Dictionary<string, PlayerCharacter> playerCharacterDictionary;

    public Dictionary<string, string> playerNameDictionary;


    [SerializeField]
    public List<PlayerInfo> playerList;



    //  private Dictionary<ulong, GameObject> playerManagerDictionary;





    private void Awake()
    {
        Instance = this;
        playerReadyDictionary = new Dictionary<ulong, bool>();
        playerTeamDictionary = new Dictionary<string, int>();
        playerCharacterDictionary = new Dictionary<string, PlayerCharacter>();
        playerNameDictionary = new Dictionary<string, string>();

        playerList = new List<PlayerInfo> { };

        DontDestroyOnLoad(gameObject);


    }

    private void Start()
    {
    }

    public override void OnNetworkSpawn()
    {
        PlayerLobbyId = AuthenticationService.Instance.PlayerId;

        Debug.Log("NETWORK SPAWN!! ");

        // playerNameDictionary.Add(PlayerLobbyId, EditPlayerName.Instance.GetPlayerName());

        //ChangeNameServerRpc(PlayerLobbyId, EditPlayerName.Instance.GetPlayerName());
        //GetPlayerNamesServerRpc(PlayerLobbyId);



        if (IsServer)
        {
            //     NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }
    }


    private void SetUpClient()
    {
        //     foreach
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetPlayerNamesServerRpc(string playerId)
    {
        foreach (KeyValuePair<string, string> name in playerNameDictionary)
        {
            if (name.Key != playerId)
            {
                Debug.Log("Servidor, diccionario sin key : " + name.Key);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetDefNameServerRpc(string playerId)
    {
        //GetPlayerById(playerId);
        GetDefNameClientRpc(playerId);
    }


    [ClientRpc]
    public void GetDefNameClientRpc(string playerId)
    {
        //GetPlayerById(playerId);
        //Debug.Log(playerId);

        ChangeNameServerRpc(playerId, EditPlayerName.Instance.GetPlayerName(), NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetTeamCharacterServerRpc(string playerId, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        Debug.Log("nnnn" + playerTeamDictionary.Count);
      /*  foreach (KeyValuePair<string, int> name in playerTeamDictionary)
        {
            Debug.Log(name.Value);
            if (name.Key != playerId)
            {
                //Estoy hay que buscarlo en el cliente no en el servidor
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                };

                ChangeTeamClientRpc(name.Key, name.Value, clientId, clientRpcParams);
                Debug.Log("Servidor, diccionario sin key : " + name.Key);
            }
        }


        foreach (KeyValuePair<string, PlayerCharacter> character in playerCharacterDictionary)
        {
            if (character.Key != playerId)
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                };
                ChangeCharacterClientRpc(character.Key, character.Value, clientId, clientRpcParams);
            }
        }*/
      foreach(PlayerInfo playerInfo in playerList)
        {
            if(playerInfo.lobbyPlayerId != playerId)
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                };
                ChangeTeamClientRpc(playerInfo.lobbyPlayerId, playerInfo.team, clientId, clientRpcParams);
                ChangeCharacterClientRpc(playerInfo.lobbyPlayerId, playerInfo.playerCharacter, clientId, clientRpcParams);

            }
        }

        // if (!playerTeamDictionary.ContainsKey(playerId))
        //   playerTeamDictionary.Add(playerId, 1);


        foreach (KeyValuePair<string, int> name in playerTeamDictionary)
        {
            Debug.Log("team dict: " + name.Key + " " + name.Value);
        }
    }



    [ServerRpc(RequireOwnership = false)]
    public void ChangeNameServerRpc(string playerId, string name, ulong clientId)
    {
        //GetPlayerById(playerId);
        ChangeNameClientRpc(playerId, name, clientId);
    }


    [ClientRpc]
    public void ChangeNameClientRpc(string playerId, string name, ulong clientId)
    {
        //GetPlayerById(playerId);
        //Debug.Log(playerId);

        /*  if (playerNameDictionary.ContainsKey(playerId))
          {
              playerNameDictionary[playerId] = name;
              Debug.Log("NAME :  " + name);
              int index = playerList.FindIndex(x => x.lobbyPlayerId == playerId);
              if(index >= 0)
              {
                  var player = playerList[index];
                  player.name = name;
                  playerList[index] = player;
              }
          }
        */
        int index = playerList.FindIndex(x => x.lobbyPlayerId == playerId);
        if (index >= 0)
        {
            playerNameDictionary[playerId] = name;
            var player = playerList[index];
            player.name = name;
            playerList[index] = player;
        }
        else
        {
            playerNameDictionary.Add(playerId, name);
            Debug.Log("UPDATED NAME :  " + name);
            PlayerInfo newPlayer = new PlayerInfo();
            newPlayer.lobbyPlayerId = playerId;
            // newPlayer.clientId = NetworkManager.Singleton.LocalClientId;
            newPlayer.clientId = clientId;
            newPlayer.team = 1;
            newPlayer.name = name;
            playerList.Add(newPlayer);
        }

        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.LobbyPlayers[playerId].UpdateNameUI(name);
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void ChangeCharacterServerRpc(string playerId, PlayerCharacter playerCharacter, ulong clientId)
    {
        //GetPlayerById(playerId);
        ChangeCharacterClientRpc(playerId, playerCharacter, clientId);
    }


    [ClientRpc]
    public void ChangeCharacterClientRpc(string playerId, PlayerCharacter playerCharacter, ulong clientId, ClientRpcParams clientRpcParams = default)
    {
        //GetPlayerById(playerId);
        //Debug.Log(playerId);

        foreach (KeyValuePair<string, PlayerCharacter> item in playerCharacterDictionary)
        {
            Debug.Log("AAAAAAAAAAAAAAAAAAAA" +  item.Value.ToString());
        }

        /* if (playerCharacterDictionary.ContainsKey(playerId))
         {
             playerCharacterDictionary[playerId] = playerCharacter;
             int index = playerList.FindIndex(x => x.lobbyPlayerId == playerId);
             Debug.Log("AAA " + index);
             if (index >= 0)
             {
                 var player = playerList[index];
                 player.playerCharacter = playerCharacter;
                 playerList[index] = player;
             }
         }
        */
        int index = playerList.FindIndex(x => x.lobbyPlayerId == playerId);
        Debug.Log("AAA " + index);
        if (index >= 0)
        {
            playerCharacterDictionary[playerId] = playerCharacter;

            var player = playerList[index];
            player.playerCharacter = playerCharacter;
            playerList[index] = player;
        }
        else
        {
            playerCharacterDictionary.Add(playerId, playerCharacter);
            PlayerInfo newPlayer = new PlayerInfo();
            newPlayer.lobbyPlayerId = playerId;
            // newPlayer.clientId = NetworkManager.Singleton.LocalClientId;
            newPlayer.clientId = clientId;
            newPlayer.team = 1;
            newPlayer.playerCharacter = playerCharacter;
            playerList.Add(newPlayer);

        }

        foreach (KeyValuePair<string, int> item in playerTeamDictionary)
        {
            //Debug.Log("AAAAAAAAAAAAAAAAAAAA" +  item.Value);
        }

        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.LobbyPlayers[playerId].UpdateCharacterUI(playerCharacter);
        }
    }



    [ServerRpc(RequireOwnership = false)]
    public void ChangeTeamServerRpc(string playerId, int team, ulong clientId)
    {
        //GetPlayerById(playerId);
        ChangeTeamClientRpc(playerId, team, clientId);
    }


    [ClientRpc]
    public void ChangeTeamClientRpc(string playerId, int team, ulong clientId, ClientRpcParams clientRpcParams = default)
    {
        //GetPlayerById(playerId);
        Debug.Log("EXECUTED HERE!!");
        Debug.Log(team);
        /*  if (playerTeamDictionary.ContainsKey(playerId))
          {
              playerTeamDictionary[playerId] = team;

              int index = playerList.FindIndex(x => x.lobbyPlayerId == playerId);
              if (index >= 0)
              {
                  var player = playerList[index];
                  player.team = team;
                  playerList[index] = player;
              }

          }
        */
        int index = playerList.FindIndex(x => x.lobbyPlayerId == playerId);
        if (index >= 0)
        {
            var player = playerList[index];
            playerTeamDictionary[playerId] = team;
            player.team = team;
            playerList[index] = player;
        }
        else
        {
            playerTeamDictionary.Add(playerId, team);
            PlayerInfo newPlayer = new PlayerInfo();
            newPlayer.lobbyPlayerId = playerId;
            //  newPlayer.clientId = NetworkManager.Singleton.LocalClientId;
            newPlayer.clientId = clientId;
            newPlayer.team = team;
            playerList.Add(newPlayer);
        }

        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.LobbyPlayers[playerId].UpdateTeamUi(team);
        }
    }


    public int GetTeam(string playerId)
    {
        if (playerTeamDictionary.ContainsKey(playerId))
            return playerTeamDictionary[playerId];
        playerTeamDictionary.Add(playerId, 0);
        return playerTeamDictionary[playerId];

    }

    public string GetName(string playerId)
    {
        if (playerNameDictionary.ContainsKey(playerId))
            return playerNameDictionary[playerId];
        playerNameDictionary.Add(playerId, EditPlayerName.Instance.GetPlayerName());
        return playerNameDictionary[playerId];
    }



    //Se tiene que llamar en el OnNetworkSpawn, se esta llamando antes que se prepare el playerTeamDictionary
    [ServerRpc]
    public void CreatePlayersServerRpc(ServerRpcParams serverRpcParams = default)
    {
        try
        {
            foreach (PlayerInfo playerInfo in playerList)
            {
                GameObject prefabInstance = LobbyAssets.Instance.GetPrefab(playerInfo.playerCharacter);
                GameObject newPlayerGameObject = (GameObject)Instantiate(prefabInstance);

                newPlayerGameObject.GetComponent<PlayerManager>().PlayerTeam = playerInfo.team;
                newPlayerGameObject.GetComponent<PlayerManager>().PlayerName = playerInfo.name;
                newPlayerGameObject.GetComponent<PlayerManager>().playerCharacterr = playerInfo.playerCharacter;
                newPlayerGameObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(playerInfo.clientId, true);

            }


            /*ulong clientId = serverRpcParams.Receive.SenderClientId;
            Debug.Log("SETUP SERVER!");
            Player newPlayerInstance = LobbyManager.Instance.GetPlayerById(PlayerLobbyId);
           // PlayerCharacter playerCharacterInstance = //Enum.Parse<PlayerCharacter>(player.Data[KEY_PLAYER_CHARACTER].Value);
            GameObject prefabInstance = LobbyAssets.Instance.GetPrefab(playerCharacterInstance);
            GameObject newPlayerGameObject = (GameObject)Instantiate(prefabInstance);

            newPlayerInstance.GetComponent<PlayerManager>().PlayerTeam = PlayerTeam;
            newPlayerInstance.GetComponent<PlayerManager>().name = PlayerName;


            newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

            Debug.Log("INSTANTIATED");

            Debug.Log(playerTeamDictionary.Count);
            foreach (KeyValuePair<ulong, int> item in playerTeamDictionary)
            {
                Debug.Log(item.Value);
            }


            ulong clientId = serverRpcParams.Receive.SenderClientId;

            Debug.Log("SETUP SERVER!");
            Player player = LobbyManager.Instance.GetPlayerById(PlayerLobbyId);
            PlayerCharacter playerCharacter = Enum.Parse<PlayerCharacter>(player.Data[KEY_PLAYER_CHARACTER].Value);
            GameObject prefab = LobbyAssets.Instance.GetPrefab(playerCharacter);
            GameObject newPlayer = (GameObject)Instantiate(prefab);
            newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            Debug.Log("SETED UP SERVER!!");
            */

        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }


            /*
            [ServerRpc]
            public void GetServerValuesServerRpc(string playerId,ulong clientId)
            {
                try
                {
                    ClientRpcParams clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { clientId }
                        }
                    };
                    GetServerValuesClientRpc(GetTeam(playerId), playerId, clientRpcParams);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
                //HAY QUE INICIALIZAR PRIMERO LOS VALORES DE LOS DICCIONARIOS
              //  return Tuple.Create(GetTeam(playerId), playerNameDictionary[playerId], playerCharacterDictionary[playerId]);
            }


            [ClientRpc]
            public void GetServerValuesClientRpc(int team, string playerId, ClientRpcParams clientRpcParams = default) //
            {
                //Se está llamando en cliente?playerNameDictionary[playerId], playerCharacterDictionary[playerId]
                //   
                Debug.Log("CHANGING VALUES FOR : 0" + playerId);
                Debug.Log("VALUES::  " + team + " " + EditPlayerName.Instance.GetPlayerName() + playerCharacterDictionary[playerId].ToString());
                LobbyUI.Instance.LobbyPlayers[playerId].SetUpTemplate(team, EditPlayerName.Instance.GetPlayerName(), playerCharacterDictionary[playerId]);
            }
            */

        }


    /*

   


    }





    */

/*
[ServerRpc]
public void SetUpVariablesServerRpc(string team, string name, ServerRpcParams serverRpcParams = default)
{
    Debug.Log("AAAAAAAAAAAA");
    //NO ESTÁ LLEGANDO

    //Localmente funciona aquí
    /* PlayerTeam = team;
     PlayerName = name;
    */
//  playerTeamDictionary[serverRpcParams.Receive.SenderClientId] = int.Parse(team);
/*
  SetPlayerReadyServerRpc();
}





/*

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

/*

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


    private void Update()
    {
        /*if (!IsServer)
        {
            return;
        }

        switch (state.Value)
        {
            case State.WaitingToStart:
                break;
            case State.CountdownToStart:
                countdownToStartTimer.Value -= Time.deltaTime;
                if (countdownToStartTimer.Value < 0f)
                {
                    state.Value = State.GamePlaying;
                    gamePlayingTimer.Value = gamePlayingTimerMax;
                }
                break;
            case State.GamePlaying:
                gamePlayingTimer.Value -= Time.deltaTime;
                if (gamePlayingTimer.Value < 0f)
                {
                    state.Value = State.GameOver;
                }
                break;
            case State.GameOver:
                break;
        
        }*/
/*    }


    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
  /*      if (state.Value == State.WaitingToStart)
        {
            isLocalPlayerReady = true;
            OnLocalPlayerReadyChanged?.Invoke(this, EventArgs.Empty);

            SetPlayerReadyServerRpc();
        }
  
    }

/*
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;

        bool allClientsReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                // This player is NOT ready
                allClientsReady = false;
                break;
            }
        }

        if (allClientsReady)
        {
            state.Value = State.CountdownToStart;
            waiting = false;
        }
    }

    private void TestGamePausedState()
    {
      /*  foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (playerPausedDictionary.ContainsKey(clientId) && playerPausedDictionary[clientId])
            {
                // This player is paused
                isGamePaused.Value = true;
                return;
            }
        }

        // All players are unpaused
        isGamePaused.Value = false;
      */

//}


//}
