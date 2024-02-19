using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using static LobbyManager;

public class LobbyUI : MonoBehaviour {


    public static LobbyUI Instance { get; private set; }


    [SerializeField] private Transform playerSingleTemplate;
    [SerializeField] private Transform playerSingleStats;

    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI killsText;

    [SerializeField] public TextMeshProUGUI gameModeText;
    [SerializeField] private Button changeMarineButton;
    [SerializeField] private Button changeNinjaButton;
    [SerializeField] private Button changeZombieButton;
    [SerializeField] private Button changeNoPredButton;

    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button changeGameModeButton;
    [SerializeField] private Button startGameButton;

    [SerializeField] public GameObject JoiningLobbyGameObject;
    [SerializeField] public TextMeshProUGUI JoiningLobbyText;
    [SerializeField] public TextMeshProUGUI codeText;


    public bool dropDownExpanded;

    [SerializeField] public Dictionary<string, LobbyPlayerSingleUI> LobbyPlayers;



    private void Awake() {
        
        Instance = this;

        playerSingleTemplate.gameObject.SetActive(false);

        changeMarineButton.onClick.AddListener(() => {
            OnlineManager.Instance.ChangeCharacterServerRpc(LobbyManager.Instance.GetPlayerOrCreate().Id, LobbyManager.PlayerCharacter.Marine, NetworkManager.Singleton.LocalClientId);
        });
        changeNinjaButton.onClick.AddListener(() => {
            OnlineManager.Instance.ChangeCharacterServerRpc(LobbyManager.Instance.GetPlayerOrCreate().Id, LobbyManager.PlayerCharacter.Ninja, NetworkManager.Singleton.LocalClientId);

        });
        changeZombieButton.onClick.AddListener(() => {
            OnlineManager.Instance.ChangeCharacterServerRpc(LobbyManager.Instance.GetPlayerOrCreate().Id, LobbyManager.PlayerCharacter.Zombie, NetworkManager.Singleton.LocalClientId);

        });

        changeNoPredButton.onClick.AddListener(() => {
            OnlineManager.Instance.ChangeCharacterServerRpc(LobbyManager.Instance.GetPlayerOrCreate().Id, LobbyManager.PlayerCharacter.NoPred, NetworkManager.Singleton.LocalClientId);

        });

        leaveLobbyButton.onClick.AddListener(async () => {
            Debug.Log("event leave lobby");
            await LobbyManager.Instance.LeaveLobby();
            //LobbyManager.Instance.PlayerLeftServerRpc(AuthenticationService.Instance.PlayerId);
        });

        changeGameModeButton.onClick.AddListener(() => {
            LobbyManager.Instance.ChangeGameMode();
        });

        startGameButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.StartGame();
        });

        LobbyPlayers = new Dictionary<string, LobbyPlayerSingleUI>();

       // DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        LobbyManager.Instance.OnJoinedLobby += SetUpLobby_Event;
        LobbyManager.Instance.OnJoinedLobbyUpdate += UpdateLobby_Event;
       // LobbyManager.Instance.OnLobbyGameModeChanged += UpdateLobby_Event2;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby; 
        LobbyManager.Instance.OnKickedFromLobby += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickPlayer += LobbyManagerKickPlayer;
        //LobbyManager.Instance.ExternalPlayerLeft += LobbyExternalPlayerLeft;


        if(!LobbyManager.Instance.joined)
            Hide();
    }

    private void OnDestroy()
    {
        LobbyManager.Instance.OnJoinedLobby -= SetUpLobby_Event;
        LobbyManager.Instance.OnJoinedLobbyUpdate -= UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby -= LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby -= LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickPlayer -= LobbyManagerKickPlayer;
    }



    private void LobbyManagerKickPlayer(object sender, String id)
    {
        DeletePlayer(id);
    }

    private void LobbyManager_OnLeftLobby(object sender, System.EventArgs e) {
        LobbyUI.Instance.Hide();
        LobbyUI.Instance.ClearLobby();
        VivoxManager.Instance.LeaveVivox();
    }

    public void UpdateLobby_Event(object sender, LobbyManager.LobbyEventArgs e) {

        string PlayerLobbyId = AuthenticationService.Instance.PlayerId;

        CreatePlayersUI(e.lobby);
        OnlineManager.Instance.ChangeNameServerRpc(PlayerLobbyId, EditPlayerName.Instance.GetPlayerName(), NetworkManager.Singleton.LocalClientId);
        OnlineManager.Instance.GetTeamCharacterServerRpc(PlayerLobbyId);

        codeText.text = "Code: " + LobbyManager.Instance.joinedLobby.LobbyCode;

        EnableDisableStartButton(LobbyManager.Instance.IsLobbyHost());
    }

    public void EnableDisableStartButton(bool autoShow = true)
    {
        if (LobbyManager.Instance.IsLobbyHost())
        {
            if (OnlineManager.Instance.playerList.Count < LobbyManager.Instance.GetJoinedLobby().Players.Count)
            {
                Instance.startGameButton.enabled = false;
                Instance.startGameButton.GetComponentInChildren<TextMeshProUGUI>().text = "Syncing players...";
                Instance.startGameButton.GetComponent<Image>().enabled = false;

            }
            else
            {
                Instance.startGameButton.enabled = true;
                Instance.startGameButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start";
                Instance.startGameButton.GetComponent<Image>().enabled = true;

            }
            Instance.LobbyPlayers[OnlineManager.Instance.PlayerLobbyId].SetTeamClickable(true);
        }
        else
        {
            if ((OnlineManager.Instance.playerList.Find(x => x.name.Length == 0) != null || !autoShow))
            {
                Instance.startGameButton.enabled = false;
                Instance.startGameButton.GetComponentInChildren<TextMeshProUGUI>().text = "Waiting for players to join...";
                Instance.startGameButton.GetComponent<Image>().enabled = false;
                Instance.LobbyPlayers[OnlineManager.Instance.PlayerLobbyId].SetTeamClickable(false);
            }
            else
            {
                Instance.startGameButton.enabled = false;
                Instance.startGameButton.GetComponentInChildren<TextMeshProUGUI>().text = "Waiting for host to start";
                Instance.startGameButton.GetComponent<Image>().enabled = false;
                Instance.LobbyPlayers[OnlineManager.Instance.PlayerLobbyId].SetTeamClickable(true);
            }

        }
    }

    private void SetUpLobby_Event(object sender, LobbyManager.LobbyEventArgs e)
    {
       //Debug.Log("SET UP EVENT");
    }




    public void CreatePlayersUI(Lobby lobby, bool sameLobby = false)
    {
        if(lobby == null)
             lobby = LobbyManager.Instance.GetJoinedLobby();
        string gameMode = lobby.Data[LobbyManager.KEY_GAME_MODE].Value;
        if (gameMode == LobbyManager.GameMode.Team_DeathMatch.ToString())
            LobbyManager.Instance.m_gameMode = LobbyManager.GameMode.Team_DeathMatch;
        else
            LobbyManager.Instance.m_gameMode = LobbyManager.GameMode.Free_for_all;


        gameModeText.text = gameMode.Replace("_", " ");

        if (sameLobby)
        {
            VivoxManager.Instance.m_vivoxUserHandlers.Clear();
        }
        foreach (Player player in lobby.Players)
        {

            if (!LobbyPlayers.ContainsKey(player.Id))
            {
                Transform playerSingleTransform = Instantiate(Instance.playerSingleTemplate, Instance.container);
                LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.gameObject.GetComponent<LobbyPlayerSingleUI>();


                lobbyPlayerSingleUI.SetId(player.Id);

                playerSingleTransform.gameObject.SetActive(true);


                lobbyPlayerSingleUI.SetKickPlayerButtonVisible(
                    LobbyManager.Instance.IsLobbyHost() &&
                    player.Id != AuthenticationService.Instance.PlayerId // Don't allow kick self
                );

                //lobbyPlayerSingleUI.SetTeamClickable(player.Id == AuthenticationService.Instance.PlayerId);

                lobbyPlayerSingleUI.SetTeamClickable(false);
                LobbyPlayers.Add(player.Id, lobbyPlayerSingleUI);
                changeGameModeButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
                lobbyNameText.text = lobby.Name;
                playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;

                //startGameButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());

                Show();
                AddUserHandler(playerSingleTransform.gameObject.GetComponent<VivoxUserHandler>());

                Debug.Log(gameMode);

                if (gameMode == GameMode.Free_for_all.ToString())
                {
                    lobbyPlayerSingleUI.selectTeamDropdown.gameObject.SetActive(false);
                }
            }
            else
            {
                if (gameMode == GameMode.Free_for_all.ToString())
                {
                    LobbyPlayers[player.Id].selectTeamDropdown.gameObject.SetActive(false);
                }
              //  LobbyPlayers[player.Id].playerNameText.text = OnlineManager.Instance.playerList.Find(x => x.lobbyPlayerId == player.Id).name.ToSafeString();

            }
        }
        lobbyNameText.text = lobby.Name;
        playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        if (VivoxManager.Instance.VivoxJoined)
            VivoxManager.Instance.AllowVolumeChange();
        EnableDisableStartButton(false);
        killsText.text = "Kills: " + OnlineManager.Instance.maxKills.Value;
    }



    public void CreateStatisticsUI()
    {
        Lobby lobby = LobbyManager.Instance.GetJoinedLobby();
        Debug.Log(lobby.Players.Count);
        foreach (Player player in lobby.Players)
        {

            if (!LobbyPlayers.ContainsKey(player.Id))
            {
                Debug.Log("client x");

                Transform playerSingleTransform = Instantiate(playerSingleStats, container);
                LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.gameObject.GetComponent<LobbyPlayerSingleUI>();


                lobbyPlayerSingleUI.SetId(player.Id);

                playerSingleTransform.gameObject.SetActive(true);


                //LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.GetComponent<LobbyPlayerSingleUI>();

                lobbyPlayerSingleUI.SetKickPlayerButtonVisible(
                    LobbyManager.Instance.IsLobbyHost() &&
                    player.Id != AuthenticationService.Instance.PlayerId // Don't allow kick self
                );
                lobbyPlayerSingleUI.SetTeamClickable(player.Id == AuthenticationService.Instance.PlayerId);
                LobbyPlayers.Add(player.Id, lobbyPlayerSingleUI);
                changeGameModeButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
                lobbyNameText.text = lobby.Name;
                playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
                gameModeText.text = lobby.Data[LobbyManager.KEY_GAME_MODE].Value;


              if(LobbyManager.Instance.IsLobbyHost())
                {
                    startGameButton.gameObject.SetActive(true);
                }
                Show();

                AddUserHandler(playerSingleTransform.gameObject.GetComponent<VivoxUserHandler>());

            }
        }
    }


    private void AddUserHandler(VivoxUserHandler playerLobbyHandler)
    {
        VivoxManager.Instance.m_vivoxUserHandlers.Add(playerLobbyHandler);
    }

    private void ClearUserHandler()
    {
        LobbyPlayers.Clear();
        VivoxManager.Instance.m_vivoxUserHandlers.Clear();
        VivoxManager.Instance.m_VivoxSetup.m_userHandlers.Clear();
        OnlineManager.Instance.playerList.Clear();
        OnlineManager.Instance.ClearLobby();
    }


    private void ClearLobby() {
        foreach (Transform child in container) {
            if (child == playerSingleTemplate) continue;

            Destroy(child.gameObject);
        }
        LobbyUI.Instance.ClearUserHandler();
    }

    public void DeletePlayer(string idToDelete)
    {
        foreach(Transform child in container)
        {
            if(child.TryGetComponent<LobbyPlayerSingleUI>(out LobbyPlayerSingleUI lobbyUI))
            {
                if(lobbyUI.playerId == idToDelete)
                {
                    Destroy(lobbyUI.gameObject);
                    LobbyPlayers.Remove(idToDelete);

                }
            }
        }

        Lobby lobby = LobbyManager.Instance.GetJoinedLobby();
        playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
    }

    private void Hide() {
        LobbyUI.Instance.gameObject.SetActive(false);
    }

    public void Show() {
        Instance.gameObject.SetActive(true);
    }

}