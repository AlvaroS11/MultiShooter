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
using System.Threading;
using Unity.Collections;
using System.Threading.Tasks;
using Unity.Services.Lobbies;

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


    public bool dropDownExpanded;

    [SerializeField] public Dictionary<string, LobbyPlayerSingleUI> LobbyPlayers;



    private void Awake() {
        
        Instance = this;

        playerSingleTemplate.gameObject.SetActive(false);

        changeMarineButton.onClick.AddListener(() => {
            // LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Marine);
            OnlineManager.Instance.ChangeCharacterServerRpc(LobbyManager.Instance.GetPlayerOrCreate().Id, LobbyManager.PlayerCharacter.Marine, NetworkManager.Singleton.LocalClientId);
        });
        changeNinjaButton.onClick.AddListener(() => {
            //    LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Ninja);
            OnlineManager.Instance.ChangeCharacterServerRpc(LobbyManager.Instance.GetPlayerOrCreate().Id, LobbyManager.PlayerCharacter.Ninja, NetworkManager.Singleton.LocalClientId);

        });
        changeZombieButton.onClick.AddListener(() => {
            //  LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Zombie);
            OnlineManager.Instance.ChangeCharacterServerRpc(LobbyManager.Instance.GetPlayerOrCreate().Id, LobbyManager.PlayerCharacter.Zombie, NetworkManager.Singleton.LocalClientId);

        });

        changeNoPredButton.onClick.AddListener(() => {
            //  LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Zombie);
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


 /*   private void LobbyExternalPlayerLeft(object sender, String id)
    {
        Debug.Log("DELETE PLAYER LOBBY UI");
        DeletePlayer(id);
    }
 */

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

    }

   /* private void InitializeUIParams(string)
    {

    }
*/
    private void UpdateLobby_Event2(object sender, LobbyManager.LobbyEventArgs e)
    {
      //  Debug.Log("LOLLOBY GAME MODE CHANGE");
       //UpdateLobby();
    }

    private void SetUpLobby_Event(object sender, LobbyManager.LobbyEventArgs e)
    {
       Debug.Log("SET UP EVENT");
      /*  string PlayerLobbyId = AuthenticationService.Instance.PlayerId;

        OnlineManager.Instance.GetTeamCharacterServerRpc(PlayerLobbyId);
      */

     //   UpdateLobby();
    }

    /*public void UpdateLobby() {
        UpdateLobby(LobbyManager.Instance.GetJoinedLobby());
    }*/




    /*[ClientRpc]
    public void ChangeGameModeTextClientRpc(string gameMode, bool showTeam)
    {
        gameModeText.text = gameMode;

        Debug.Log(gameMode);

        foreach(var playerUI in LobbyPlayers)
        {
            playerUI.Value.selectTeamDropdown.gameObject.active = showTeam;

          //  playerUI.Value.selectTeamDropdown.enabled = showTeam;
        }
    }*/


    public void CreatePlayersUI(Lobby lobby)
    {
        if(lobby == null)
             lobby = LobbyManager.Instance.GetJoinedLobby();
        string gameMode = lobby.Data[LobbyManager.KEY_GAME_MODE].Value;
        if (gameMode == LobbyManager.GameMode.Team_DeathMatch.ToString())
            LobbyManager.Instance.m_gameMode = LobbyManager.GameMode.Team_DeathMatch;
        else
            LobbyManager.Instance.m_gameMode = LobbyManager.GameMode.Free_for_all;


        gameModeText.text = gameMode.Replace("_", " ");
        foreach (Player player in lobby.Players)
        {

            if (!LobbyPlayers.ContainsKey(player.Id))
            {
                Transform playerSingleTransform = Instantiate(playerSingleTemplate, container);
                LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.gameObject.GetComponent<LobbyPlayerSingleUI>();


                lobbyPlayerSingleUI.SetId(player.Id);

                playerSingleTransform.gameObject.SetActive(true);


                lobbyPlayerSingleUI.SetKickPlayerButtonVisible(
                    LobbyManager.Instance.IsLobbyHost() &&
                    player.Id != AuthenticationService.Instance.PlayerId // Don't allow kick self
                );

                lobbyPlayerSingleUI.SetTeamClickable(player.Id == AuthenticationService.Instance.PlayerId);
                LobbyPlayers.Add(player.Id, lobbyPlayerSingleUI);
                changeGameModeButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
                lobbyNameText.text = lobby.Name;
                playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
                if(LobbyManager.Instance.IsLobbyHost())
                    startGameButton.gameObject.SetActive(true);
                Show();
                AddUserHandler(playerSingleTransform.gameObject.GetComponent<VivoxUserHandler>());

                if (gameMode == GameMode.Free_for_all.ToString())
                {
                    lobbyPlayerSingleUI.selectTeamDropdown.gameObject.active = false;
                }


            }
        }
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


              /*  if (lobby.Players.Count == lobby.MaxPlayers && LobbyManager.Instance.IsLobbyHost())
                    startGameButton.gameObject.SetActive(true);
                else
                {
                    startGameButton.gameObject.SetActive(true);

                }*/
              if(LobbyManager.Instance.IsLobbyHost())
                {
                    startGameButton.gameObject.SetActive(true);
                }
                Show();

                AddUserHandler(playerSingleTransform.gameObject.GetComponent<VivoxUserHandler>());


                /* if (LobbyManager.Instance.IsLobbyHost())
                 {
                     string PlayerLobbyId = AuthenticationService.Instance.PlayerId;
                     OnlineManager.Instance.AddToList(PlayerLobbyId);
                 }
               */
                //VivoxManager.Instance.m_vivoxUserHandlers.Add(playerSingleTransform.gameObject.GetComponent<VivoxUserHandler>());

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

    public void UpdateUITeam()
    {

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

        //Lobby lobby = await LobbyService.Instance.GetLobbyAsync(LobbyManager.Instance.joinedLobby.Id);
        Lobby lobby = LobbyManager.Instance.GetJoinedLobby();
        Debug.Log(lobby.Players.Count);
        playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        Debug.Log(lobby.Players.Count + "/" + lobby.MaxPlayers);
    }

    private void Hide() {
        Debug.Log("hide");
        LobbyUI.Instance.gameObject.SetActive(false);
    }

    public void Show() {
        gameObject.SetActive(true);
    }

}