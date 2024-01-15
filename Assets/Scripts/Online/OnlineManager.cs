using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LobbyManager;
using System;
using Unity.Services.Authentication;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Collections;
//using UnityEditor.PackageManager;
using UnityEngine.Jobs;
using Unity.VisualScripting;
using System.Linq;

public class OnlineManager : NetworkBehaviour
{

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


    public Dictionary<string, int> playerTeamDictionary;

    public Dictionary<string, PlayerCharacter> playerCharacterDictionary;

    public Dictionary<string, string> playerNameDictionary;


    [SerializeField]
    public List<PlayerInfo> playerList;



    [SerializeField]
    public NetworkList<int> teamScore;

    [SerializeField]
    public NetworkList<int> teamNames;

    [SerializeField]
    private List<TextMeshProUGUI> teamScoreTexts;

    [SerializeField]
    private Transform scoreCounterPrefab;

    // [SerializeField]
    //private NetworkList<int> teamSpawn = new NetworkList<int>();

    [SerializeField]
    private GameObject spawnParent;

    [SerializeField]
    public List<Transform> spawnPoints;


    [SerializeField] private TextMeshProUGUI team1;
    [SerializeField] private TextMeshProUGUI team2;



    [SerializeField] private int timeToRespawn = 3;

    [SerializeField]
    public int inmuneTime = 5;

    public NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    //public int maxKills = 10;

    public NetworkVariable<int> maxKills = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    public bool playersCreated = false;


    //[SerializeField]
    //private int ownTeam;

    //  private Dictionary<ulong, GameObject> playerManagerDictionary;


    // [SerializeField]
    //public List <PlayerManager> playerManagers;

    private HashSet<PlayerManager> playerManagers = new HashSet<PlayerManager>();

    private Scene m_LoadedScene;

    public bool SceneIsLoaded
    {
        get
        {
            if (m_LoadedScene.IsValid() && m_LoadedScene.isLoaded)
            {
                return true;
            }
            return false;
        }
    }



    private EndGame endGame;

    private GameObject messageGameObject;
    private TextMeshProUGUI messageText;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        playerTeamDictionary = new Dictionary<string, int>();
        playerCharacterDictionary = new Dictionary<string, PlayerCharacter>();
        playerNameDictionary = new Dictionary<string, string>();

        playerList = new List<PlayerInfo> { };

        DontDestroyOnLoad(gameObject);

      //  NetworkManager.Singleton.SceneManager.OnLoadComplete += OnlineManager_CreatePlayers;

    }

    private void OnlineManager_CreatePlayers(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log(sceneName);
        //if(sceneName == SceneLoader.Scene.GameScene.ToString())
       // CreatePlayersServerRpc();
    }

    private void Start()
    {
        teamScore = new NetworkList<int>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        teamNames = new NetworkList<int>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

        LobbyManager.Instance.OnLeftLobby += StopClient;
        LobbyManager.Instance.OnJoinedLobby += StartClient;
        LobbyManager.Instance.OnKickedFromLobby += StopClient;
    }

    private void StartClient(object sender, System.EventArgs e)
    {

    }

    private void StopClient(object sender, System.EventArgs e)
    {
        NetworkManager.Singleton.Shutdown();
    }

    public override void OnNetworkSpawn()
    {
        PlayerLobbyId = AuthenticationService.Instance.PlayerId;

        StartCoroutine(DelayJoin());

        if (IsServer)
        {
            //     NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }

        NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;

        if (IsClient)
        {
            teamNames.OnListChanged += OnClientListChanged;
        }
    }

    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        var clientOrServer = sceneEvent.ClientId == NetworkManager.ServerClientId ? "server" : "client";
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.LoadComplete:
                {
                    // We want to handle this for only the server-side
                    if (sceneEvent.ClientId == NetworkManager.ServerClientId)
                    {
                        // *** IMPORTANT ***
                        // Keep track of the loaded scene, you need this to unload it
                        m_LoadedScene = sceneEvent.Scene;
                    }
                    Debug.Log($"Loaded the {sceneEvent.SceneName} scene on " +
                        $"{clientOrServer}-({sceneEvent.ClientId}).");
                    break;
                }
            case SceneEventType.UnloadComplete:
                {
                    Debug.Log($"Unloaded the {sceneEvent.SceneName} scene on " +
                        $"{clientOrServer}-({sceneEvent.ClientId}).");
                    break;
                }
            case SceneEventType.LoadEventCompleted:
                {
                    //All clients have changed from scene
                    if (IsServer && !playersCreated)
                    {
                        Debug.Log("All clients joined");
                        messageGameObject = GameAssets.Instance.messageGameObject;
                        messageText = GameAssets.Instance.messageText;
                        messageText.text = "Waiting for players...";
                        messageGameObject.SetActive(true);
                        CreatePlayersServerRpc();
                        SetStatustClientRpc(true);

                        //messageGameObject.SetActive(false);

                        ShowMessageClientRpc(false);
                        gameStarted.Value = true;
                        playersCreated = true;
                    }
                    break;
                }
            case SceneEventType.UnloadEventCompleted:
                {
                    var loadUnload = sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted ? "Load" : "Unload";
                    Debug.Log($"{loadUnload} event completed for the following client " +
                        $"identifiers:({sceneEvent.ClientsThatCompleted})");
                    if (sceneEvent.ClientsThatTimedOut.Count > 0)
                    {
                        Debug.LogWarning($"{loadUnload} event timed out for the following client " +
                            $"identifiers:({sceneEvent.ClientsThatTimedOut})");
                    }
                    break;
                }
        }
    }

    [ClientRpc]
    public void SetStatustClientRpc(bool started)
    {
        playersCreated = started;
    }

    public IEnumerator DelayJoin()
    {
        yield return new WaitForSeconds(1.5f);
        LobbyUI.Instance.JoiningLobbyGameObject.SetActive(false);
    }

    [ClientRpc]
    public void ShowMessageClientRpc(bool show)
    {
        if (messageGameObject == null)
            messageGameObject = GameAssets.Instance.messageGameObject;
        messageGameObject.SetActive(show);
    }

    /*[ServerRpc(RequireOwnership = false)]
    public void GetDefNameServerRpc(string playerId)
    {
        //GetPlayerById(playerId);
        GetDefNameClientRpc(playerId);
    }
    */


    /*  [ClientRpc]
      public void GetDefNameClientRpc(string playerId)
      {
          //GetPlayerById(playerId);
          //Debug.Log(playerId);

          ChangeNameServerRpc(playerId, EditPlayerName.Instance.GetPlayerName(), NetworkManager.Singleton.LocalClientId);
      }
    */

    [ServerRpc(RequireOwnership = false)]
    public void GetTeamCharacterServerRpc(string playerId, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        foreach (PlayerInfo playerInfo in playerList)
        {
            if (playerInfo.lobbyPlayerId != playerId)
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
                break;

            }
        }
    }



    [ServerRpc(RequireOwnership = false)]
    public void ChangeNameServerRpc(string playerId, FixedString128Bytes name, ulong clientId)
    {
        ChangeNameClientRpc(playerId, name, clientId);
    }


    [ClientRpc]
    public void ChangeNameClientRpc(string playerId, FixedString128Bytes name, ulong clientId)
    {
        int index = playerList.FindIndex(x => x.lobbyPlayerId == playerId);
        if (index >= 0)
        {
            //playerNameDictionary[playerId] = name;
            var player = playerList[index];
            player.name = name;
            playerList[index] = player;
        }
        else
        {
            PlayerInfo newPlayer = new PlayerInfo();
            newPlayer.lobbyPlayerId = playerId;
            // newPlayer.clientId = NetworkManager.Singleton.LocalClientId;
            newPlayer.clientId = clientId;
            newPlayer.team = 1;
            newPlayer.name = name;
            playerList.Add(newPlayer);
            //return;
        }

        if (LobbyUI.Instance != null)
        {
            //Check
            try
            {
                LobbyUI.Instance.LobbyPlayers[playerId].UpdateNameUI(name);
            }catch(Exception e)
            {
                Debug.LogWarning("Cannot update name due to error: " + e);
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void ChangeCharacterServerRpc(string playerId, PlayerCharacter playerCharacter, ulong clientId)
    {
        ChangeCharacterClientRpc(playerId, playerCharacter, clientId);
    }


    [ClientRpc]
    public void ChangeCharacterClientRpc(FixedString128Bytes playerId, PlayerCharacter playerCharacter, ulong clientId, ClientRpcParams clientRpcParams = default)
    {
        
        int index = playerList.FindIndex(x => x.lobbyPlayerId == playerId);
        //Debug.Log("AAA " + index);
        if (index >= 0)
        {
            var player = playerList[index];
            player.playerCharacter = playerCharacter;
            playerList[index] = player;
        }
        else
        {
            PlayerInfo newPlayer = new PlayerInfo();
            newPlayer.lobbyPlayerId = playerId;
            newPlayer.clientId = clientId;
            newPlayer.team = 1;
            newPlayer.playerCharacter = playerCharacter;

         //   return;
            playerList.Add(newPlayer);
        }

        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.LobbyPlayers[playerId.ToString()].UpdateCharacterUI(playerCharacter);
        }
    }



    [ServerRpc(RequireOwnership = false)]
    public void ChangeTeamServerRpc(string playerId, int team, ulong clientId)
    {
        //GetPlayerById(playerId);
        ChangeTeamClientRpc(playerId, team, clientId);
    }


    [ClientRpc]
    public void ChangeTeamClientRpc(FixedString128Bytes playerId, int team, ulong clientId, ClientRpcParams clientRpcParams = default)
    {
        int index = playerList.FindIndex(x => x.lobbyPlayerId == playerId);
        if (index >= 0)
        {
            var player = playerList[index];
            player.team = team;
            playerList[index] = player;
        }
        else
        {
            PlayerInfo newPlayer = new PlayerInfo();
            newPlayer.lobbyPlayerId = playerId;
            newPlayer.clientId = clientId;
            newPlayer.team = team;
            playerList.Add(newPlayer);
            //return;
        }

        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.LobbyPlayers[playerId.ToString()].UpdateTeamUi(team);
        }
    }

    //HOST ONLY
    public void AddToList(FixedString128Bytes playerId)
    {
        PlayerInfo newPlayer = new PlayerInfo();
        newPlayer.lobbyPlayerId = playerId;
        playerList.Add(newPlayer);
    }




    public int GetTeam(string playerId)
    {
        return playerList.Find(player => player.lobbyPlayerId == playerId).team;

    }

    public void ClearLobby()
    {
        LobbyUI.Instance.LobbyPlayers.Clear();
    }


    [ClientRpc]
    public void DeletePlayerLobbyIdClientRpc(string playerId)
    {
        Debug.Log(playerList.Count);
        playerList.Remove(playerList.Find(x => x.lobbyPlayerId.ToSafeString() == playerId));
        LobbyUI.Instance.DeletePlayer(playerId);
        Debug.Log(playerList.Count);

    }

    [ServerRpc (RequireOwnership = false)]
     public void DeletePlayerLobbyIdServerRpc(string playerId, ServerRpcParams serverRpcParams = default)
     {
        LobbyManager.Instance.joinedLobby.Players.Remove(LobbyManager.Instance.joinedLobby.Players.Find(x => x.Id == playerId));

        DeletePlayerLobbyIdClientRpc(playerId);

    }


    //Se tiene que llamar en el OnNetworkSpawn, se esta llamando antes que se prepare el playerTeamDictionary
    [ServerRpc]
    public void CreatePlayersServerRpc(ServerRpcParams serverRpcParams = default)
    {
        try
        {
            int nTeams = 0;
            while (SceneManager.GetActiveScene().name != "GameScene") { }

            spawnParent = GameObject.Find("SpawnPoints");
            System.Random rand = new System.Random();

            nTeams = playerList.DistinctBy(dd => dd.team).Count();

            ResetPreviousGame();
            //ResetPreviousGameClientRpc();

            int[] teamNames1 = new int[nTeams];
            int i = 0;
            foreach (PlayerInfo playerInfo in playerList)
            {
                if(LobbyManager.Instance.m_gameMode == LobbyManager.GameMode.Free_for_all)
                {
                    teamScore.Add(0);
                    teamNames.Add(playerInfo.team);
                  //  teamNames1[i] = playerInfo.team;
                    SetPlayerSpawns(Mathf.Min( teamScore.Count - 1, 8)); //8 is MaxPlayers
                    i++;
                }
                if(!teamNames.Contains(playerInfo.team))
                {
                    teamScore.Add(0);
                    teamNames.Add(playerInfo.team);
                    teamNames1[i] = playerInfo.team;
                    SetPlayerSpawns(teamScore.Count - 1);
                    i++;
                }

                GameObject prefabInstance = GameAssets.Instance.GetPrefab(playerInfo.playerCharacter);

                int ran = rand.Next(1, 2);
                int clampledTeam = Mathf.Clamp(playerInfo.team, 0, teamScore.Count - 1)*2; //real team spawn from 1 to n of teams
                int randomIndex = rand.Next(clampledTeam, clampledTeam+2);
                Debug.Log(randomIndex);
                Transform randomSpawn = spawnPoints[randomIndex];
                GameObject newPlayerGameObject = (GameObject)Instantiate(prefabInstance, randomSpawn);


                PlayerManager newPlayerManager = newPlayerGameObject.GetComponent<PlayerManager>();

                newPlayerManager.PlayerTeam.Value = playerInfo.team;
                newPlayerManager.PlayerName.Value = playerInfo.name;
                newPlayerManager.PlayerLobbyId.Value = playerInfo.lobbyPlayerId;

                newPlayerManager.playerCharacterr = playerInfo.playerCharacter;
                newPlayerManager.PlayerInfoIndex = playerList.IndexOf(playerInfo);

                playerList.Find(x => x.clientId == playerInfo.clientId).playerObject = newPlayerGameObject;
               
                newPlayerGameObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(playerInfo.clientId, true);
            }
            Debug.Log(teamScore.Count);

            StartTeamScoreClientRpc(teamNames1);

            TestClientRpc(teamNames1);

            setPlayerLifeBarsClientRpc();


            GameAssets.Instance.stats.GetComponent<StatisticsUI>().InitializeStatisticsClientRpc();
            endGame = GameAssets.Instance.endGame;
            Time.timeScale = 1;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            messageGameObject.SetActive(true);
            messageText.text = "Fatal error, please restart the game";
        }
    }

    [ClientRpc]
    public void ResetPreviousGameClientRpc()
    {
        //if (!IsServer)
          //  return;
        teamNames.Clear();
        teamScore.Clear();
        playerManagers.Clear();
        teamScoreTexts.Clear();
    }

    public void ResetPreviousGame()
    {
        teamNames.Clear();
        teamScore.Clear();
        spawnPoints.Clear();
        playerManagers.Clear();
        teamScoreTexts.Clear();
    }

    [ClientRpc]
    public void TestClientRpc(int[] testArray)
    {
        Debug.Log("···");
        foreach (var test in testArray)
            Debug.Log(test);
    }

    void OnServerListChanged(NetworkListEvent<int> changeEvent)
    {
        Debug.Log($"[S] The list changed and now has {teamNames.Count} elements");
    }

    void OnClientListChanged(NetworkListEvent<int> changeEvent)
    {
        Debug.Log($"[S] The list changed and now has {teamNames.Count} elements");
    }

    //Recorre todos los jugadores
    [ClientRpc]
    public void setPlayerLifeBarsClientRpc()
    {

        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        playerManagers.Clear();

        int ownTeam = -1;
        foreach (GameObject playerObject in playerObjects)
        {
            PlayerManager playerManager = playerObject.GetComponent<PlayerManager>();
            playerManagers.Add(playerManager);

            if(playerManager.isOwnPlayer)
                ownTeam = playerManager.PlayerTeam.Value;
        }

        if (LobbyManager.Instance.m_gameMode == LobbyManager.GameMode.Free_for_all)
        {
            Debug.Log("Free for all lifeBars");
            foreach (PlayerManager playerManager in playerManagers)
            {
                if (!playerManager.isOwnPlayer)
                {
                    playerManager.healthUI.healthBar.color = Color.red;
                }
            }
        }
        else
        {
            Debug.Log(LobbyManager.Instance.m_gameMode);
            foreach (PlayerManager playerManager in playerManagers)
            {

                if (ownTeam != playerManager.PlayerTeam.Value)
                {
                    playerManager.healthUI.healthBar.color = Color.red;
                }
            }
        }

        if (ownTeam == -1)
            throw new Exception("FALLO EL HASH");

        Debug.Log("playersLifeBar");

    }

    [ClientRpc]
    public void ChangeGameModeTextClientRpc(string gameMode, bool showTeam)
    {
        LobbyUI.Instance.gameModeText.text = gameMode;
        if (!showTeam)
            LobbyManager.Instance.m_gameMode = GameMode.Free_for_all;
        else
            LobbyManager.Instance.m_gameMode = GameMode.Team_DeathMatch;

        Debug.Log(LobbyManager.Instance.m_gameMode.ToString());
        foreach (var playerUI in LobbyUI.Instance.LobbyPlayers)
        {
            playerUI.Value.selectTeamDropdown.gameObject.active = showTeam;
        }
    }

 

    private void SetPlayerSpawns(int teamIndex)
    {
        Debug.Log(teamIndex);
        GameObject spawnTeam = spawnParent.transform.GetChild(teamIndex).gameObject;
        Debug.Log(spawnTeam.name);
        for (int i = 0; i < 3; i++)
        {
            spawnPoints.Add(spawnTeam.transform.GetChild(i));//  GET CHILDREN
        }
    }

    [ClientRpc]
    private void StartTeamScoreClientRpc(int[] nTeams)
    {
        teamScoreTexts.Clear();
        Transform container = Assets.Instance.teamContainer;
        scoreCounterPrefab = Assets.Instance.scoreCounterPrefab;

        //while(teamNames.Count != nTeams){
        for(int i = 0; i< nTeams.Length; i++)
        {
            Transform newCounterText = Instantiate(scoreCounterPrefab, container);
            newCounterText.gameObject.SetActive(true);
            TextMeshProUGUI counter = newCounterText.GetComponent<TextMeshProUGUI>();
            counter.text = "Team " + nTeams[i] + ":  0";
            teamScoreTexts.Add(counter);
        }

        scoreCounterPrefab.gameObject.SetActive(false);

        //PONER AL CAMBIAR DE ESCENA Y DINÁMICO!
        //  team1 = GameObject.Find("Team1").GetComponent<TextMeshProUGUI>();
        //team2 = GameObject.Find("Team2").GetComponent<TextMeshProUGUI>();
    }



    [ServerRpc]
    public void ChangeScoreServerRpc(int shooter, int hitted) 
    {
        Debug.Log("change score, index: " + shooter);
        teamScore[shooter]++;
        ChangeScoreClientRpc(shooter, teamScore[shooter], playerList[shooter].lobbyPlayerId, playerList[hitted].lobbyPlayerId);

        if (teamScore[shooter] >= maxKills.Value)
        {
            endGame.ShowEndGameServer(teamNames[shooter], playerList[shooter].name.ToString());
        }
    }

    [ClientRpc]
    public void ChangeScoreClientRpc(int shooter, int score, FixedString128Bytes shooterId, FixedString128Bytes hittedId )
    {
        if(LobbyManager.Instance.m_gameMode == LobbyManager.GameMode.Team_DeathMatch)
            teamScoreTexts[shooter].text = "Team " + (teamNames[shooter]) + ": " + score;

        PlayerInfo pShooter = playerList.Find(pl => pl.lobbyPlayerId == shooterId);
        pShooter.kills += 1;
        PlayerInfo pHitted = playerList.Find(pl => pl.lobbyPlayerId == hittedId);
        pHitted.deaths += 1;

        pShooter.UpdateStats();
        pHitted.UpdateStats();
    }

    //Server only
    public void PlayerDeath(ulong playerId)
    {
        PlayerInfo playerInfo = playerList.Find(x => x.clientId == playerId);

        GameObject playerObj = playerInfo.playerObject;
        System.Random rand = new System.Random();


        int randomIndex = rand.Next(playerInfo.team, (playerInfo.team + 2));

        Transform randomSpawn = spawnPoints[randomIndex];

        playerObj.transform.position = randomSpawn.position;

        playerObj.SetActive(false);
        PlayerDeathClientRpc(playerInfo.lobbyPlayerId);

        StartCoroutine(WaitToRespawn(playerObj, playerInfo.clientId));

    }

    [ClientRpc]
    //public void PlayerDeathClientRpc(FixedString128Bytes playerName)
    public void PlayerDeathClientRpc(FixedString128Bytes lobbyPlayerId)
    {
        PlayerInfo playerInfo = playerList.Find(x => x.lobbyPlayerId == lobbyPlayerId);
        GameObject playerObj = playerInfo.playerObject;
        playerObj.SetActive(false);
        RespawnMessage(playerObj.GetComponent<PlayerManager>());
    }


    [ClientRpc]
    public void PlayerAliveClientRpc(FixedString128Bytes lobbyPlayerId, bool active = true)
    {
        PlayerInfo playerInfo = playerList.Find(x => x.lobbyPlayerId == lobbyPlayerId);
        GameObject playerObj = playerInfo.playerObject;
        playerObj.SetActive(true);
        Assets.Instance.respawnMsg.SetActive(false);
        PlayerManager pManager = playerObj.GetComponent<PlayerManager>();
        pManager.bodyAnimator.SetBool("inmuneBool", true);


    }


    //Client only
    private void RespawnMessage(PlayerManager pManager)
    {
        if (!pManager.isOwnPlayer)
            return;

        StartCoroutine(UpdateRespawnText());
        Assets.Instance.respawnMsg.SetActive(true);


    }

    //Client only
    private IEnumerator UpdateRespawnText()
    {
        for(int i = 0; i< timeToRespawn; i++)
        {
            yield return new WaitForSeconds(1);

            Assets.Instance.respawnText.text = "Respawning in " + (timeToRespawn - i - 1);

            Debug.Log("Respawning in " + (timeToRespawn - i));
        }

        Assets.Instance.respawnText.text =  "Respawning... ";



    }

    //Server only
    private IEnumerator WaitToRespawn(GameObject player, ulong clientId)
    {
        yield return new WaitForSeconds(timeToRespawn);

        PlayerAliveClientRpc(playerList.Find(x => x.clientId == clientId).lobbyPlayerId);
        PlayerManager playerDead = player.GetComponent<PlayerManager>();
        playerDead.isInmune.Value = true;
        playerDead.life.Value = playerDead.MaxLife;
        StartCoroutine(InmuneTime(clientId));
    }

    //Server only
    private IEnumerator InmuneTime(ulong clientId)
    {
        PlayerManager p1 = playerList.Find(x => x.clientId == clientId).playerObject.GetComponent<PlayerManager>();

        yield return new WaitForSeconds(inmuneTime);
        p1.isInmune.Value = false;

        p1.bodyAnimator.SetBool("inmuneBool", false);


        PlayerStopInmuneClientRpc(playerList.Find(x => x.clientId == clientId).lobbyPlayerId);

    }

    [ClientRpc]
    public void PlayerStopInmuneClientRpc(FixedString128Bytes lobbyPlayerId)
    {
        PlayerManager p1 = playerList.Find(x => x.lobbyPlayerId == lobbyPlayerId).playerObject.GetComponent<PlayerManager>();
       // p1.animator.SetBool("inmuneBool", false);
        p1.bodyAnimator.SetBool("inmuneBool", false);
    }
}
