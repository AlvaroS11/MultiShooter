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
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;

using static LobbyManager;


public class LobbyManager : MonoBehaviour {


    public static LobbyManager Instance { get; private set; }


    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_CHARACTER = "Character";
    public const string KEY_GAME_MODE = "GameMode";
   // public const string KEY_START_GAME = "StartGame";

    public const string KEY_RELAY_CODE = "RelayCode";

    public const string KEY_PLAYER_TEAM = "0";




    public event EventHandler OnLeftLobby;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
   // public event EventHandler<LobbyEventArgs> OnLobbyGameModeChanged;
    public event EventHandler<String> OnKickPlayer;
    public event EventHandler<String> ExternalPlayerLeft;


    public class LobbyEventArgs : EventArgs {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs {
        public List<Lobby> lobbyList;
    }


    public enum GameMode {
        Team_DeathMatch,
        Free_for_all
    }

    public enum PlayerCharacter {
        Marine,
        Ninja,
        Zombie,
        NoPred
    }

    public PlayerCharacter IntToCharacter(int index)
    {
        switch(index)
        {
            case 0: return PlayerCharacter.Marine;
            case 1: return PlayerCharacter.Ninja;
            case 2: return PlayerCharacter.Zombie;
            default: return PlayerCharacter.Marine;
        }
    }

    public int CharacterToInt(PlayerCharacter index)
    {
        switch (index)
        {
            case PlayerCharacter.Marine: return 0;
            case PlayerCharacter.Ninja: return 1;
            case PlayerCharacter.Zombie: return 2;
            default: return 0;
        }
    }



    private float heartbeatTimer;
    private float lobbyPollTimer;
    private float refreshLobbyListTimer = 5f;
    public Lobby joinedLobby;
    private string playerName;

    private int maxPlayers;
    private int maxKills;


    public GameObject LobbyCanvas;


    //Re join lobby
    public bool joined = false;

    // public OnlineManager onlineManager;

    public GameMode m_gameMode;


    private void Awake() {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }

    }

    private void Update() {
        //HandleRefreshLobbyList(); // Disabled Auto Refresh for testing with multiple builds
        //PERIODIC TASKS (5seconds)
        HandleLobbyHeartbeat();
        HandleLobbyPolling();

        NetworkManager.Singleton.OnTransportFailure += HandleTransportFailure;
    }

    private void HandleTransportFailure()
    {
        LeaveLobby();
    }

    public async void Authenticate(string playerName) {

        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var stringChars = new char[8];
        var random = new System.Random();

        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        var finalString = new String(stringChars);

        this.playerName = playerName +"_" + finalString;
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(this.playerName);



        //VivoxService.Instance.Initialize();


        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () => {

            RefreshLobbyList();
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(OnlineManager.Instance.IsClient)
            LoadScene(scene, mode);
    }

    async void LoadScene(Scene scene, LoadSceneMode mode)
    //   public IEnumerator LoadScene(Scene scene, LoadSceneMode mode)
    {
        // yield return new WaitForSeconds(0f);

        if (Instance == null)
        {
            Destroy(gameObject);
        }
        else
        {
            if (joinedLobby != null && scene.name.ToString() == SceneLoader.Scene.LobbyScene.ToString())
            {
                EditPlayerName.Instance.SetPlayerName(playerName);
                AuthenticateUI.Instance.gameObject.SetActive(false);
                LobbyListUI.Instance.gameObject.SetActive(false);
                LobbyUI.Instance.Show();
                LobbyUI.Instance.CreatePlayersUI(joinedLobby, true);
                LobbyUI.Instance.JoiningLobbyGameObject.SetActive(false);
                joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    IsLocked = false,
                });

                LobbyUI.Instance.EnableDisableStartButton(false);
                OnlineManager.Instance.SetStatustClientRpc(false);

            }
        }
    }


    private async void HandleLobbyHeartbeat() {
        if (IsLobbyHost()) {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f) {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;
                if (joinedLobby == null)
                    return;
                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling() {
        if (joinedLobby != null) {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f) {
                float lobbyPollTimerMax = 4f;
                lobbyPollTimer = lobbyPollTimerMax;

                if (IsPlayerInLobby())
                {
                    try
                    {
                        joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                    }
                    catch(LobbyServiceException e)
                    {
                        //The lobby has been eliminated or the Unity Services are down
                        await LeaveLobby();
                        Debug.LogWarning("Leaving lobby due to error: " + e.Message);
                        return;
                    }
                }

                else
                {
                    // Player was kicked out of this lobby

                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                    joinedLobby = null;
                }


                if (SceneManager.GetActiveScene().name == SceneLoader.Scene.LobbyScene.ToString())
                {
                    OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
                    LobbyUI.Instance.EnableDisableStartButton();
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

            bool showTeam = true;
            switch (gameMode) {
                default:
                case GameMode.Team_DeathMatch:
                    gameMode = GameMode.Free_for_all;
                    showTeam = false;
                    break;
                case GameMode.Free_for_all:
                    gameMode = GameMode.Team_DeathMatch;
                    break;
            }

            UpdateLobbyGameMode(gameMode);
            OnlineManager.Instance.ChangeGameModeTextClientRpc(gameMode.ToString().Replace("_", " "), showTeam);
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
            Debug.LogError(e);
            return default;
        }
    }

    private async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers); //pass players

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);


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
     
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

            RelayServerData relayData = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);

            NetworkManager.Singleton.StartClient();
           // LobbyCanvas.SetActive(false);
            return joinAllocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            LobbyCanvas.SetActive(true);
            return default;
        }
    }

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode, int maxKills) {


        this.maxPlayers = maxPlayers;
        this.maxKills = maxKills;

        string code = await CreateRelay();

        NetworkManager.Singleton.StartHost();
        joined = true;
        Player player = CreatePlayer();

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            Player = player,
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject> {
                { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) },
                { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, code)  },
            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        joinedLobby = lobby;


        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

        //TODO ADD IN EVENT OnJoinedLobby
        VivoxManager.Instance.StartVivoxLogin();
        OnlineManager.Instance.maxKills.Value = maxKills;

        StartCoroutine(VivoxManager.Instance.WaitForJoin());
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

    public async void JoinLobby(Lobby lobby) {
        Player player = CreatePlayer();

        Debug.Log(player.Id);

        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions {
            Player = player
        });

        await JoinRelay(joinedLobby.Data[KEY_RELAY_CODE].Value);
        joined = true;

        VivoxManager.Instance.StartVivoxLogin();
        VivoxManager.Instance.StartVivoxJoin();

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public async void JoinLobbyByCode(string code)
    {
        Player player = CreatePlayer();

        joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, new JoinLobbyByCodeOptions
        {
            Player = player
        });

        await JoinRelay(joinedLobby.Data[KEY_RELAY_CODE].Value);
        joined = true;

        //TODO ADD IN EVENT OnJoinedLobby
        VivoxManager.Instance.StartVivoxLogin();
        VivoxManager.Instance.StartVivoxJoin();

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
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
                Debug.LogError(e);
            }
        }
    }

    public async void StartGame()
    {
        if (joinedLobby.Players.Count == joinedLobby.MaxPlayers && LobbyManager.Instance.IsLobbyHost())
        {
            ForceStart();
        }
        else if (IsLobbyHost() && PopUp.Instance != null)
        {
            PopUp.Instance.ShowPopUp("There are " + (joinedLobby.MaxPlayers - joinedLobby.Players.Count) + " free spaces \n" +
                "Do you want to start the game?", true, PopUp.PopUpType.Info);

            PopUp.OnButton1Click += OnCancelStart;
            PopUp.OnButton2Click += OnConfirmStart;
        }
    }

    private void OnCancelStart()
    {
      //  PopUp.Instance.gameObject.SetActive(false);
        PopUp.OnButton2Click -= OnConfirmStart;
        PopUp.OnButton1Click -= OnCancelStart;
    }

    private void OnConfirmStart()
    {
        //  PopUp.Instance.gameObject.SetActive(false);
        ForceStart();
        PopUp.OnButton2Click -= OnConfirmStart;
        PopUp.OnButton1Click -= OnCancelStart;
    }

    private async void ForceStart()
    {
        try
        {
            SceneLoader.LoadNetwork(SceneLoader.Scene.GameScene);

            //Enviar mensaje para decir que hemos empezado
            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                IsLocked = true,
               // IsPrivate = true,
            });

            joinedLobby = lobby;
             
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
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


    public async Task LeaveLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                //If is server remove server
                if (OnlineManager.Instance.IsServer)
                    await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                    joined = false;
                    m_gameMode = GameMode.Team_DeathMatch;
                    OnlineManager.Instance.DeletePlayerLobbyIdServerRpc(AuthenticationService.Instance.PlayerId);
                    joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                }
                

            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
            }
            finally
            {
                joined = false;
                joinedLobby = null;
                OnLeftLobby?.Invoke(this, EventArgs.Empty);
                VivoxManager.Instance.LeaveVivox();
                Debug.LogError("Leaving Lobby due to transport failure");
            }
        }
    }
      
    
    public async void KickPlayer(string playerId) {
        if (IsLobbyHost()) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
                joinedLobby.Players.Remove(joinedLobby.Players.Find(x => x.Id == playerId));
                OnlineManager.Instance.DeletePlayerLobbyIdServerRpc(playerId);

                OnKickPlayer?.Invoke(this, playerId);

            }
            catch (LobbyServiceException e) {
                Debug.Log(e);
                LobbyUI.Instance.DeletePlayer(playerId);
            }
        }
    }

public int GetTeam(string playerId)
    {
        if (playerId == AuthenticationService.Instance.PlayerId)
        {
            // return  GetPlayerOrCreate().Data[LobbyManager.KEY_PLAYER_TEAM].Value;
            int team = OnlineManager.Instance.GetTeam(playerId);
            return team;
        }
        return -1;
    }

    public async void UpdateLobbyGameMode(GameMode gameMode) {
        try {            
            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
                }
            });

            joinedLobby = lobby;

            m_gameMode = gameMode;
          //  OnLobbyGameModeChanged?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

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