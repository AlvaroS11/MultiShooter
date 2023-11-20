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
using Unity.Collections;
public class LobbyUI : MonoBehaviour {


    public static LobbyUI Instance { get; private set; }


    [SerializeField] private Transform playerSingleTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private Button changeMarineButton;
    [SerializeField] private Button changeNinjaButton;
    [SerializeField] private Button changeZombieButton;
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

        leaveLobbyButton.onClick.AddListener(() => {
            LobbyManager.Instance.LeaveLobby();
        });

        changeGameModeButton.onClick.AddListener(() => {
            LobbyManager.Instance.ChangeGameMode();
        });

        startGameButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.StartGame();
        });

        LobbyPlayers = new Dictionary<string, LobbyPlayerSingleUI>();
    }

    private void Start() {
        LobbyManager.Instance.OnJoinedLobby += SetUpLobby_Event;
        LobbyManager.Instance.OnJoinedLobbyUpdate += UpdateLobby_Event;
        LobbyManager.Instance.OnLobbyGameModeChanged += UpdateLobby_Event2;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby; 
        LobbyManager.Instance.OnKickedFromLobby += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickPlayer += LobbyManagerKickPlayer;


        Hide();
    }

    private void LobbyManagerKickPlayer(object sender, String id)
    {
        DeletePlayer(id);
    }

    private void LobbyManager_OnLeftLobby(object sender, System.EventArgs e) {
        ClearLobby();
        Hide();
    }

    private void UpdateLobby_Event(object sender, LobbyManager.LobbyEventArgs e) {
     //   Debug.Log("OnJoinedLobbyUpdate");

        //  UpdateLobby();
        string PlayerLobbyId = AuthenticationService.Instance.PlayerId;

        CreatePlayersUI();
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
       // UpdateLobby();
    }

    private void SetUpLobby_Event(object sender, LobbyManager.LobbyEventArgs e)
    {
      //  Debug.Log("SET UP EVENT");

     //   UpdateLobby();
    }

    /*public void UpdateLobby() {
        UpdateLobby(LobbyManager.Instance.GetJoinedLobby());
    }*/




    private void OnClientConnected(ulong clientId)
    {
        Debug.Log(clientId);
        Debug.Log("CLIENT CONNECTED  ");
      /*  if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Client connected. You can now call ServerRpc methods.");
            // Call your ServerRpc method here or trigger an event to notify other scripts
            CallYourServerRpc();
        }
      */
    }




    //Client Only
    
    /*private void UpdateLobby(Lobby lobby) {
        //ClearLobby();


        //METER EN ONCLIENT SPAWN O ALGOS ASI
        foreach (Player player in lobby.Players) {

            LobbyPlayerSingleUI lobbyPlayerSingleUI = null;

            if (LobbyPlayers.ContainsKey(player.Id))
            {
                //update
                //lobbyPlayerSingleUI = LobbyPlayers[player.Id];
            }
            else
            {
                Debug.Log("creating... ");
                Transform playerSingleTransform = Instantiate(playerSingleTemplate, container);
                playerSingleTransform.gameObject.GetComponent<LobbyPlayerSingleUI>().playerId = player.Id;

                Debug.Log(playerSingleTemplate.GetComponent<LobbyPlayerSingleUI>().playerId);

                playerSingleTransform.gameObject.SetActive(true);

                
                lobbyPlayerSingleUI = playerSingleTransform.GetComponent<LobbyPlayerSingleUI>();

                lobbyPlayerSingleUI.SetKickPlayerButtonVisible(
                    LobbyManager.Instance.IsLobbyHost() &&
                    player.Id != AuthenticationService.Instance.PlayerId // Don't allow kick self
                );

                lobbyPlayerSingleUI.SetTeamClickable(player.Id == AuthenticationService.Instance.PlayerId);

                LobbyPlayers.Add(player.Id, lobbyPlayerSingleUI);

                //lobbyPlayerSingleUI.playerId = player.Id;

                lobbyPlayerSingleUI.SetId(player.Id);

                Debug.Log("ddd" + lobbyPlayerSingleUI.player);



                //                OnlineManager.Instance.GetServerValuesServerRpc(player.Id, clientId: NetworkManager.Singleton.ConnectedClientsIds[i]);
                //   (team, name, playerCharacter) = OnlineManager.Instance.GetServerValuesServerRpc(player.Id);

                // lobbyPlayerSingleUI.SetUpTemplate(team, name, playerCharacter);

            }
        }

        changeGameModeButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());

        lobbyNameText.text = lobby.Name;
        playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        gameModeText.text = lobby.Data[LobbyManager.KEY_GAME_MODE].Value;

        if (lobby.Players.Count == lobby.MaxPlayers && LobbyManager.Instance.IsLobbyHost())
            startGameButton.gameObject.SetActive(true);
        else
            startGameButton.gameObject.SetActive(false);

        Show();
    }

    */

    public void CreatePlayersUI()
    {
        Lobby lobby = LobbyManager.Instance.GetJoinedLobby();
        //Debug.Log(lobby);
        foreach (Player player in lobby.Players)
        {
            if (!LobbyPlayers.ContainsKey(player.Id))
            {
                Transform playerSingleTransform = Instantiate(playerSingleTemplate, container);
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

                /*if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    lobbyPlayerSingleUI.DesactivateSound();
                }*/

                if (lobby.Players.Count == lobby.MaxPlayers && LobbyManager.Instance.IsLobbyHost())
                    startGameButton.gameObject.SetActive(true);
                else
                    startGameButton.gameObject.SetActive(false);

                Show();

                VivoxManager.Instance.m_vivoxUserHandlers.Add(playerSingleTransform.gameObject.GetComponent<VivoxUserHandler>());
            }
        }
    }


    public void UpdateUITeam()
    {

    }

    private void ClearLobby() {
        foreach (Transform child in container) {
            if (child == playerSingleTemplate) continue;

            Destroy(child.gameObject);
        }
    }

    private void DeletePlayer(string idToDelete)
    {
        foreach(Transform child in container)
        {
            if(child.TryGetComponent<LobbyPlayerSingleUI>(out LobbyPlayerSingleUI lobbyUI))
            {
                if(lobbyUI.playerId == idToDelete)
                    Destroy(lobbyUI.gameObject);
            }
        }
        Lobby lobby = LobbyManager.Instance.GetJoinedLobby();
        playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        Debug.Log(lobby.Players.Count + "/" + lobby.MaxPlayers);

    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void Show() {
        gameObject.SetActive(true);
    }

}