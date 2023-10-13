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



    //  private Dictionary<ulong, GameObject> playerManagerDictionary;





    private void Awake()
    {
        Instance = this;
        playerReadyDictionary = new Dictionary<ulong, bool>();
        playerTeamDictionary = new Dictionary<string, int>();
        playerCharacterDictionary = new Dictionary<string, PlayerCharacter>();
        playerNameDictionary = new Dictionary<string, string>();

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
        foreach(KeyValuePair<string, string> name in playerNameDictionary)
        {
            if(name.Key != playerId)
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

        ChangeNameServerRpc(playerId, EditPlayerName.Instance.GetPlayerName());
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetTeamCharacterServerRpc(string playerId, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        Debug.Log("nnnn" + playerTeamDictionary.Count);
        foreach (KeyValuePair<string, int> name in playerTeamDictionary)
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

                ChangeTeamClientRpc(name.Key, name.Value, clientRpcParams);
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
                ChangeCharacterClientRpc(character.Key, character.Value, clientRpcParams);
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
    public void ChangeNameServerRpc(string playerId, string name)
    {
        //GetPlayerById(playerId);
        ChangeNameClientRpc(playerId, name);
    }


    [ClientRpc]
    public void ChangeNameClientRpc(string playerId, string name)
    {
        //GetPlayerById(playerId);
        //Debug.Log(playerId);

        if (playerNameDictionary.ContainsKey(playerId))
        {
            playerNameDictionary[playerId] = name;
            Debug.Log("NAME :  " + name);
        }
        else
        {
            playerNameDictionary.Add(playerId, name);
            Debug.Log("UPDATED NAME :  " + name);

        }

        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.LobbyPlayers[playerId].UpdateNameUI(name);
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void ChangeCharacterServerRpc(string playerId, PlayerCharacter playerCharacter)
    {
        //GetPlayerById(playerId);
        ChangeCharacterClientRpc(playerId, playerCharacter);
    }


    [ClientRpc]
    public void ChangeCharacterClientRpc(string playerId, PlayerCharacter playerCharacter, ClientRpcParams clientRpcParams = default)
    {
        //GetPlayerById(playerId);
        //Debug.Log(playerId);

        if (playerCharacterDictionary.ContainsKey(playerId))
        {
            playerCharacterDictionary[playerId] = playerCharacter;
        }
        else
        {
            playerCharacterDictionary.Add(playerId, playerCharacter);
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
    public void ChangeTeamServerRpc(string playerId, int team)
    {
        //GetPlayerById(playerId);
        ChangeTeamClientRpc(playerId, team);
    }


    [ClientRpc]
    public void ChangeTeamClientRpc(string playerId, int team, ClientRpcParams clientRpcParams = default)
    {
        //GetPlayerById(playerId);
        Debug.Log("EXECUTED HERE!!");

        if (playerTeamDictionary.ContainsKey(playerId))
        {
            playerTeamDictionary[playerId] = team;
        }
        else
        {
            playerTeamDictionary.Add(playerId, team);
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
        playerNameDictionary.Add(playerId,EditPlayerName.Instance.GetPlayerName());
        return playerNameDictionary[playerId];
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


//Se tiene que llamar en el OnNetworkSpawn, se esta llamando antes que se prepare el playerTeamDictionary
[ServerRpc(RequireOwnership = false)]
public void SetUpPlayerServerRpc(ServerRpcParams serverRpcParams = default)
{
  try
  {
      /*  ulong clientId = serverRpcParams.Receive.SenderClientId;
        Debug.Log("SETUP SERVER!");
        Player player = LobbyManager.Instance.GetPlayerById(PlayerLobbyId);
        PlayerCharacter playerCharacter = Enum.Parse<PlayerCharacter>(player.Data[KEY_PLAYER_CHARACTER].Value);
        GameObject prefab = LobbyAssets.Instance.GetPrefab(playerCharacter);
        GameObject newPlayer = (GameObject)Instantiate(prefab);

        newPlayer.GetComponent<PlayerManager>().PlayerTeam = PlayerTeam;
        newPlayer.GetComponent<PlayerManager>().name = PlayerName;


        newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        Debug.Log("INSTANTIATED");

        Debug.Log(playerTeamDictionary.Count);
        foreach (KeyValuePair<ulong, int> item in playerTeamDictionary)
        {
            Debug.Log(item.Value);
        }
      */
/*
      ulong clientId = serverRpcParams.Receive.SenderClientId;

      Debug.Log("SETUP SERVER!");
      Player player = LobbyManager.Instance.GetPlayerById(PlayerLobbyId);
      PlayerCharacter playerCharacter = Enum.Parse<PlayerCharacter>(player.Data[KEY_PLAYER_CHARACTER].Value);
      GameObject prefab = LobbyAssets.Instance.GetPrefab(playerCharacter);
      GameObject newPlayer = (GameObject)Instantiate(prefab);
      newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
      Debug.Log("SETED UP SERVER!!");

  }
  catch (Exception e)
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
//}
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
