using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using static LobbyManager;

public class LobbyManager : MonoBehaviour {


    public static LobbyManager Instance { get; private set; }


    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_CHARACTER = "Character";
    public const string KEY_GAME_MODE = "GameMode";
    public const string KEY_START_GAME = "StartGame";

    public const string KEY_RELAY_CODE = "RelayCode";

    public const string KEY_PLAYER_TEAM = "0";




    public event EventHandler OnLeftLobby;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbyGameModeChanged;
    public class LobbyEventArgs : EventArgs {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs {
        public List<Lobby> lobbyList;
    }


    public enum GameMode {
        CaptureTheFlag,
        Conquest
    }

    public enum PlayerCharacter {
        Marine,
        Ninja,
        Zombie
    }



    private float heartbeatTimer;
    private float lobbyPollTimer;
    private float refreshLobbyListTimer = 5f;
    private Lobby joinedLobby;
    private string playerName;

    private int maxPlayers;


    public GameObject LobbyCanvas;

    // public OnlineManager onlineManager;

    public PlayerCharacter playerSelected;



    private void Awake() {
        Instance = this;
    }

    private void Update() {
        //HandleRefreshLobbyList(); // Disabled Auto Refresh for testing with multiple builds
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    public async void Authenticate(string playerName) {
        this.playerName = playerName;
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);

        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () => {
            // do nothing
            Debug.Log("Signed in! " + AuthenticationService.Instance.PlayerId);

            RefreshLobbyList();
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void HandleRefreshLobbyList() {
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn) {
            refreshLobbyListTimer -= Time.deltaTime;
            if (refreshLobbyListTimer < 0f) {
                float refreshLobbyListTimerMax = 5f;
                refreshLobbyListTimer = refreshLobbyListTimerMax;

                RefreshLobbyList();
            }
        }
    }

    private async void HandleLobbyHeartbeat() {
        if (IsLobbyHost()) {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f) {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                Debug.Log("Heartbeat");
                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling() {
        if (joinedLobby != null) {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f) {
                float lobbyPollTimerMax = 1.1f;
                lobbyPollTimer = lobbyPollTimerMax;

                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                if (!IsPlayerInLobby()) {
                    // Player was kicked out of this lobby
                    Debug.Log("Kicked from Lobby!");

                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                    joinedLobby = null;
                }

                if (joinedLobby.Data[KEY_START_GAME].Value != "0")
                {

                    Debug.Log("HOST STARTED GAME!"); 
                    //Unirnos al p2p y empezar
                    if (!IsLobbyHost()) //Host automatically joins relay
                    {
                        // await JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                     //   SceneLoader.LoadNetwork(SceneLoader.Scene.GameScene);

                    }

                    joinedLobby = null;

                   // OnGameStarted?
                }
                else
                {
                   // Debug.Log(joinedLobby.Data[KEY_START_GAME].Value);
                   // Debug.Log(joinedLobby.Data[KEY_PLAYER_CHARACTER].Value);

                }


            }
        }
    }

    public Lobby GetJoinedLobby() {
        return joinedLobby;
    }

    public bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private bool IsPlayerInLobby() {
        if (joinedLobby != null && joinedLobby.Players != null) {
            foreach (Player player in joinedLobby.Players) {
                if (player.Id == AuthenticationService.Instance.PlayerId) {
                    // This player is in this lobby
                    return true;
                }
            }
        }
        return false;
    }

    public void logPlayer()
    {
        //BIEN SOLO EN SERVIDOR
        Player player = GetPlayerOrCreate();
            Debug.Log("THIS ARE THE PLAYERS! :");
            Debug.Log(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value);
            Debug.Log(player.Data[LobbyManager.KEY_PLAYER_TEAM].Value);
            Debug.Log(player.Data[LobbyManager.KEY_PLAYER_NAME].Value);
        
    }

    public void logPlayerS()
    {
        //BIEN SOLO EN SERVIDOR
        foreach (Player player in GetJoinedLobby().Players)
        {
            Debug.Log("THIS ARE THE PLAYERS! :");
            Debug.Log(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value);
            Debug.Log(player.Data[LobbyManager.KEY_PLAYER_TEAM].Value);
            Debug.Log(player.Data[LobbyManager.KEY_PLAYER_NAME].Value);
        }
    }

    public Player GetPlayerOrCreate()
    {
        if (joinedLobby != null && joinedLobby.Players != null)
        {
            foreach (Player player in joinedLobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    // This player is in this lobby
                    return player;
                }
            }
        }
        return CreatePlayer(); ;
    }


    public Player GetPlayerById(string playerId)
    {
        if (joinedLobby != null && joinedLobby.Players != null)
        {
            foreach (Player player in joinedLobby.Players)
            {
                if (player.Id == playerId)
                {
                    // This player is in this lobby
                    return player;
                }
            }
        }
        return CreatePlayer(); ;
    }

    private Player CreatePlayer() {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
            { KEY_PLAYER_CHARACTER, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerCharacter.Marine.ToString()) },
            { KEY_PLAYER_TEAM, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "-1") }
        });
    }

    public void ChangeGameMode() {
        if (IsLobbyHost()) {
            GameMode gameMode =
                Enum.Parse<GameMode>(joinedLobby.Data[KEY_GAME_MODE].Value);

            switch (gameMode) {
                default:
                case GameMode.CaptureTheFlag:
                    gameMode = GameMode.Conquest;
                    break;
                case GameMode.Conquest:
                    gameMode = GameMode.CaptureTheFlag;
                    break;
            }

            UpdateLobbyGameMode(gameMode);
        }
    }


    private async Task<Allocation> AllocateRelay(int maxPlayers)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);

            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }

    private async Task<string> CreateRelay()
    {
        try
        {
            Debug.Log(AuthenticationService.Instance.PlayerId);
            //  OnlineManager.Instance.PlayerLobbyId = "ddd";
            //OnlineManager.Instance.PlayerLobbyId = AuthenticationService.Instance.PlayerId;

            Debug.Log(maxPlayers);

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers); //pass players

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            Debug.Log("HOST STARTING GAME!!");


      //      SceneLoader.LoadNetwork(SceneLoader.Scene.CharacterSelectScene);

       //     OnlineManager.Instance.SetUpVariablesServerRpc(GetPlayerOrCreate().Data[KEY_PLAYER_TEAM].Value, GetPlayerOrCreate().Data[KEY_PLAYER_NAME].Value);


            //  LobbyCanvas.SetActive(false);


            return joinCode;
        }
        catch(RelayServiceException e)
        {
            Debug.Log(e);
            LobbyCanvas.SetActive(true);

            return null;
        }
    }

    private async Task<JoinAllocation> JoinRelay(string code)
    {
        try
        {
            Debug.Log(AuthenticationService.Instance.PlayerId);

            Debug.Log("joining Relay " + code);
     
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

            RelayServerData relayData = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);



            // OnlineManager.Instance.PlayerTeam = GetPlayerOrCreate().Data[KEY_PLAYER_TEAM].Value;

            //OnlineManager.Instance.SetPlayerReadyServerRpc();

            NetworkManager.Singleton.StartClient();

//            OnlineManager.Instance.SetUpVariablesServerRpc(GetPlayerOrCreate().Data[KEY_PLAYER_TEAM].Value, GetPlayerOrCreate().Data[KEY_PLAYER_NAME].Value);


            Debug.Log("STARTING CLIENT");

           // LobbyCanvas.SetActive(false);
            return joinAllocation;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            LobbyCanvas.SetActive(true);
            return default;
        }
    }

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode) {


        /*  Allocation allocation = await AllocateRelay(maxPlayers);

          NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));*/

        this.maxPlayers = maxPlayers;

        string code = await CreateRelay();
        NetworkManager.Singleton.StartHost();


        Player player = CreatePlayer();


        CreateLobbyOptions options = new CreateLobbyOptions
        {
            Player = player,
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject> {
                { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) },
                { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0") },
                { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, code)  },
           //     { KEY_PLAYER_CHARACTER, new DataObject(DataObject.VisibilityOptions.Public, PlayerCharacter.Marine.ToString()) } // ESTA BIEN?? O DEBERÍA SER PLAYERDATAOBJECT
            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        joinedLobby = lobby;


        //  SceneLoader.LoadNetwork(SceneLoader.Scene.GameScene);

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

        Debug.Log("Created Lobby " + lobby.Name);
    }

    public async void RefreshLobbyList() {
        try {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter> {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder> {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

  /*  public async void JoinLobbyByCode(string lobbyCode) {
        Player player = GetPlayer();

        Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions {
            Player = player
        });

        joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }
  */

    public async void JoinLobby(Lobby lobby) {
        Player player = CreatePlayer();

        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions {
            Player = player
        });

        await JoinRelay(joinedLobby.Data[KEY_RELAY_CODE].Value);

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public async void UpdatePlayerName(string playerName) {
        this.playerName = playerName;

        if (joinedLobby != null) {
            try {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_NAME, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerName)
                    }
                };

                string playerId = AuthenticationService.Instance.PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void UpdatePlayerCharacter(PlayerCharacter playerCharacter) {
        if (joinedLobby != null) {
            try {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_CHARACTER, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerCharacter.ToString())
                    }
                };

                string playerId = AuthenticationService.Instance.PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;

                Debug.Log("*******");
                Debug.Log(playerCharacter.ToString());
              //  Debug.Log(joinedLobby.Data[KEY_PLAYER_CHARACTER].Value);
            //  Debug.Log(LobbyService.Instance.Play

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void StartGame()
    {
        //StartHost()
        //SceneLoader.LoadNetwork(SceneLoader.Scene.GameScene);

        if (IsLobbyHost())
        {
            try
            {
                Debug.Log("StartGame");
                SceneLoader.LoadNetwork(SceneLoader.Scene.GameScene);

                //Enviar mensaje para decir que hemos empezado
                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "1") }
                    }
                });
                
                //Initialize Lobby Start Game Key-Value to 0, then to Relay code
                joinedLobby = lobby;

                Debug.Log(joinedLobby.Data[KEY_START_GAME].Value);
                
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void QuickJoinLobby() {
        try {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            joinedLobby = lobby;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void LeaveLobby() {
        if (joinedLobby != null) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                joinedLobby = null;

                OnLeftLobby?.Invoke(this, EventArgs.Empty);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId) {
        if (IsLobbyHost()) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void ChangeTeam(string playerId, string team)
    {
        if(playerId == AuthenticationService.Instance.PlayerId)
        {
            try
            {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_TEAM, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: team)
                    }
                };

                //string playerId = AuthenticationService.Instance.PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;

                Debug.Log("*******");
                Debug.Log(GetPlayerOrCreate().Data[LobbyManager.KEY_PLAYER_TEAM].Value);

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public string GetTeam(string playerId)
    {
        if (playerId == AuthenticationService.Instance.PlayerId)
        {
            return  GetPlayerOrCreate().Data[LobbyManager.KEY_PLAYER_TEAM].Value;
        }
        return "-1";
    }

    public async void UpdateLobbyGameMode(GameMode gameMode) {
        try {
            Debug.Log("UpdateLobbyGameMode " + gameMode);
            
            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
                }
            });

            joinedLobby = lobby;

            OnLobbyGameModeChanged?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            return relayJoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }

    public void TestLogUserData()
    {
        Debug.Log(GetPlayerOrCreate().Data[KEY_PLAYER_TEAM].Value);
    }

}