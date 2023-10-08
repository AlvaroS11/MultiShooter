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

    private Dictionary<ulong, int> playerTeamDictionary;





    private void Awake() {
        Instance = this;
        playerReadyDictionary = new Dictionary<ulong, bool>();
        playerTeamDictionary = new Dictionary<ulong, int>();


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
        SetUpPlayerServerRpc();
    }

    [ServerRpc]
    public void SetUpVariablesServerRpc(string team, string name, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log("AAAAAAAAAAAA");
        //NO ESTÁ LLEGANDO

        //Localmente funciona aquí
        /* PlayerTeam = team;
         PlayerName = name;
        */
        playerTeamDictionary[serverRpcParams.Receive.SenderClientId] = int.Parse(team);

        SetPlayerReadyServerRpc();
    }


    //Se tiene que llamar en el OnNetworkSpawn, se esta llamando antes que se prepare el playerTeamDictionary
    [ServerRpc(RequireOwnership = false)]
    public void SetUpPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        try
        {
            ulong clientId = serverRpcParams.Receive.SenderClientId;
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
    }


    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
  /*      if (state.Value == State.WaitingToStart)
        {
            isLocalPlayerReady = true;
            OnLocalPlayerReadyChanged?.Invoke(this, EventArgs.Empty);

            SetPlayerReadyServerRpc();
        }
  */
    }


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
    }

}
