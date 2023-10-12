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

    public bool dropDownExpanded;

    [SerializeField] public Dictionary<string, LobbyPlayerSingleUI> LobbyPlayers;




    private void Awake() {
        Instance = this;

        playerSingleTemplate.gameObject.SetActive(false);

        changeMarineButton.onClick.AddListener(() => {
            // LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Marine);
            OnlineManager.Instance.ChangeCharacterServerRpc(LobbyManager.Instance.GetPlayerOrCreate().Id, LobbyManager.PlayerCharacter.Marine);
        });
        changeNinjaButton.onClick.AddListener(() => {
            //    LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Ninja);
            OnlineManager.Instance.ChangeCharacterServerRpc(LobbyManager.Instance.GetPlayerOrCreate().Id, LobbyManager.PlayerCharacter.Ninja);

        });
        changeZombieButton.onClick.AddListener(() => {
            //  LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Zombie);
            OnlineManager.Instance.ChangeCharacterServerRpc(LobbyManager.Instance.GetPlayerOrCreate().Id, LobbyManager.PlayerCharacter.Zombie);

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
        LobbyManager.Instance.OnLobbyGameModeChanged += UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby += LobbyManager_OnLeftLobby;

        Hide();
    }

    private void LobbyManager_OnLeftLobby(object sender, System.EventArgs e) {
        ClearLobby();
        Hide();
    }

    private void UpdateLobby_Event(object sender, LobbyManager.LobbyEventArgs e) {
        UpdateLobby();
    }

    private void SetUpLobby_Event(object sender, LobbyManager.LobbyEventArgs e)
    {
        UpdateLobby();
    }

    private void UpdateLobby() {
        UpdateLobby(LobbyManager.Instance.GetJoinedLobby());
    }




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
    
    private void UpdateLobby(Lobby lobby) {
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

                //    int team;
                //  string name;
                ///PlayerCharacter playerCharacter;
                ///

                OnlineManager.Instance.ChangeTeamServerRpc(player.Id, 1);
                OnlineManager.Instance.ChangeCharacterClientRpc(player.Id, PlayerCharacter.Marine);
                OnlineManager.Instance.ChangeNameClientRpc(player.Id, "dd");
                //El nombre debería de cogerse desde EditPlayerName antes


                OnlineManager.Instance.GetServerValuesServerRpc(player.Id);
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




    public void UpdateUITeam()
    {

    }

    private void ClearLobby() {
        foreach (Transform child in container) {
            if (child == playerSingleTemplate) continue;

            //if(child.name == "Team") continue;
            Destroy(child.gameObject);
        }
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void Show() {
        gameObject.SetActive(true);
    }

}