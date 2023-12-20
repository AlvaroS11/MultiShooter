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
    public event EventHandler<LobbyEventArgs> OnLobbyGameModeChanged;
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
        CaptureTheFlag,
        Conquest
    }

    public enum PlayerCharacter {
        Marine,
        Ninja,
        Zombie,
        NoPred
    }



    private float heartbeatTimer;
    private float lobbyPollTimer;
    private float refreshLobbyListTimer = 5f;
    public Lobby joinedLobby;
    private string playerName;

    private int maxPlayers;


    public GameObject LobbyCanvas;


    //Re join lobby
    public bool joined = false;

    // public OnlineManager onlineManager;



    private void Awake() {

        if (Instance == null)
        {
            // Si no hay instancia, esta será la instancia y no se destruirá al cambiar de escena
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            // Si ya hay una instancia, destruye este objeto para evitar duplicados
            Destroy(gameObject);
        }

    }

    private void Update() {
        //HandleRefreshLobbyList(); // Disabled Auto Refresh for testing with multiple builds
        //PERIODIC TASKS (5seconds)
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    public async void Authenticate(string playerName) {
        this.playerName = playerName;
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);



        //VivoxService.Instance.Initialize();


        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () => {

            RefreshLobbyList();
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //if()
        if(OnlineManager.Instance.IsClient)
            LoadScene(scene, mode);
      //  StartCoroutine(LoadScene(scene, mode));
        /*yield return new WaitForSeconds(1f);

        if (Instance == null)
        {
            Destroy(gameObject);
        }
        Debug.Log(joined);
        Debug.Log("onSceneLoaded");
        Debug.Log(joinedLobby);
      //  Debug.Log(GetJoinedLobby());
       // Debug.Log(GetJoinedLobby().Players.Count);
        Debug.Log(scene.name.ToString() == SceneLoader.Scene.LobbyScene.ToString());
        Debug.Log(scene.name.ToString());
        if (joinedLobby != null && scene.name.ToString() == SceneLoader.Scene.LobbyScene.ToString())
        {
            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            Debug.Log(joinedLobby.Players.Count);
            Debug.Log("scene reLoad");
            AuthenticateUI.Instance.gameObject.SetActive(false);
            LobbyListUI.Instance.gameObject.SetActive(false);
            Debug.Log(joinedLobby.Players.Count);
            LobbyUI.Instance.UpdateLobby_Event(null, new LobbyEventArgs { lobby = joinedLobby });
        }*/
    }

    void LoadScene(Scene scene, LoadSceneMode mode)
    //   public IEnumerator LoadScene(Scene scene, LoadSceneMode mode)
    {
       // yield return new WaitForSeconds(0f);

        if (Instance == null)
        {
            Destroy(gameObject);
        }
        else
        {
            Debug.Log(joined);
            Debug.Log("onSceneLoaded");
            Debug.Log(joinedLobby);
            //  Debug.Log(GetJoinedLobby());
            // Debug.Log(GetJoinedLobby().Players.Count);
            Debug.Log(scene.name.ToString() == SceneLoader.Scene.LobbyScene.ToString());
            Debug.Log(scene.name.ToString());
            if (joinedLobby != null && scene.name.ToString() == SceneLoader.Scene.LobbyScene.ToString())
            {
                //OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
                Debug.Log(joinedLobby.Players.Count);
                Debug.Log("scene reLoad");
                EditPlayerName.Instance.SetPlayerName(playerName);
                AuthenticateUI.Instance.gameObject.SetActive(false);
                LobbyListUI.Instance.gameObject.SetActive(false);
                Debug.Log(joinedLobby.Players.Count);
                Debug.Log("upadteEvent1");
                //LobbyUI.Instance.UpdateLobby_Event(null, new LobbyEventArgs { lobby = joinedLobby });
                //LobbyUI.Instance.CreatePlayersUI(joinedLobby);
                Debug.Log("upadtedLobby");
                LobbyUI.Instance.Show();
              //  OnlineManager.Instance.ResetPreviousGameClientRpc();
                StartCoroutine(OnlineManager.Instance.DelayJoin());

            }
        }
    }


    /* public void BackToLobby()
     {
         if(joinedLobby != null)
         OnJoinedLobby.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
     }*/

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

                if(SceneManager.GetActiveScene().name == SceneLoader.Scene.LobbyScene.ToString() )
                    OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

               /* if (joinedLobby.Data[KEY_START_GAME].Value != "0")
                {
                    //Unirnos al p2p y empezar
                    if (!IsLobbyHost()) //Host automatically joins relay
                    {
                        // await JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                     //   SceneLoader.LoadNetwork(SceneLoader.Scene.GameScene);

                    }
                    Debug.Log("joined lobby is Null");
                    joinedLobby = null;

                   // OnGameStarted?
                }
                else
                {
                   // Debug.Log(joinedLobby.Data[KEY_START_GAME].Value);
                   // Debug.Log(joinedLobby.Data[KEY_PLAYER_CHARACTER].Value);

                }*/


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

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode) {


        this.maxPlayers = maxPlayers;

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
                //{ KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0") },
                { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, code)  },
           //     { KEY_PLAYER_CHARACTER, new DataObject(DataObject.VisibilityOptions.Public, PlayerCharacter.Marine.ToString()) } // ESTA BIEN?? O DEBERÍA SER PLAYERDATAOBJECT
            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        joinedLobby = lobby;


        //  SceneLoader.LoadNetwork(SceneLoader.Scene.GameScene);

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

        //TODO ADD IN EVENT OnJoinedLobby
        VivoxManager.Instance.StartVivoxLogin();

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

        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions {
            Player = player
        });

        await JoinRelay(joinedLobby.Data[KEY_RELAY_CODE].Value);
        joined = true;

        //TODO ADD IN EVENT OnJoinedLobby
        VivoxManager.Instance.StartVivoxLogin();
        VivoxManager.Instance.StartVivoxJoin();

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
                Debug.LogError(e);
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
                SceneLoader.LoadNetwork(SceneLoader.Scene.GameScene);

                //Enviar mensaje para decir que hemos empezado
                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                      //  { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "1") }
                    }
                });
                
                //Initialize Lobby Start Game Key-Value to 0, then to Relay code
                joinedLobby = lobby;
                // test with NetworkManager.Singleton.SceneManager.OnLoadComplete
              //  OnlineManager.Instance.CreatePlayersServerRpc();                
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

    /*public async Task LeaveLobby() {
        if (joinedLobby != null) {
            try {
                VivoxManager.Instance.LeaveVivox();

                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joined = false;
                OnlineManager.Instance.DeletePlayerLobbyIdServerRpc(AuthenticationService.Instance.PlayerId);
                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

               // OnKickPlayer?.Invoke(this, playerId);
               //   ExternalPlayerLeft?.Invoke(this, AuthenticationService.Instance.PlayerId);

               OnLeftLobby?.Invoke(this, EventArgs.Empty);


            } catch (LobbyServiceException e) {
                Debug.LogError(e);
            }
        }
    }*/


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
                    OnlineManager.Instance.DeletePlayerLobbyIdServerRpc(AuthenticationService.Instance.PlayerId);
                    joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                }
                // OnKickPlayer?.Invoke(this, playerId);
                //   ExternalPlayerLeft?.Invoke(this, AuthenticationService.Instance.PlayerId);

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
            }
        }
    }
      
    
    public async void KickPlayer(string playerId) {
        if (IsLobbyHost()) {
            try {
                Debug.Log("HECHANDO!!");
                Debug.Log(joinedLobby.Players.Count);
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
                joinedLobby.Players.Remove(joinedLobby.Players.Find(x => x.Id == playerId));
                OnlineManager.Instance.DeletePlayerLobbyIdServerRpc(playerId);

                ///await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id)
                
                Debug.Log(joinedLobby.Players.Count);
               // joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                OnKickPlayer?.Invoke(this, playerId);

            }
            catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public void ChangeTeam(string playerId, string team)
    {
        /* if(playerId == AuthenticationService.Instance.PlayerId)
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
        */
    //  OnlineManager.Instance.ChangeTeamServerRpc(playerId, int.Parse(team));
}

/*   [ServerRpc(RequireOwnership = false)]
   public void ChangeTeamServerRpc(ServerRpcParams serverRpcParams = default)
   {

       //Crear una lista de los jugadores (mirar La lista de templates por ej)
       try {
           ulong clientId = serverRpcParams.Receive.SenderClientId;

       }
   }
*/



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