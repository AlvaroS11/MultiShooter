using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;



public class StatisticsUI : NetworkBehaviour
{

    public static StatisticsUI Instance { get; private set; }

    [SerializeField] private Transform playerSingleStats;

    [SerializeField] private Transform container;

    [SerializeField] private Button showStatsMobile;


    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

        showStatsMobile.onClick.AddListener(() => {
            container.gameObject.SetActive(!container.gameObject.activeSelf);
        });

#if UNITY_STANDALONE_WIN
        if (IsOwner)
            {
            showStatsMobile.gameObject.SetActive(false);
            }
#endif

        Hide();
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_STANDALONE_WIN
        if (Input.GetKey(KeyCode.Tab))
        {
            Show();
        }
        else
            Hide();

#elif UNITY_ANDROID
        //add button to show/hide stats

#endif

    }


    [ClientRpc]
    public void InitializeStatisticsClientRpc()
    {
        try
        {
        Debug.Log("INITIALIZING STATISTICS");
            foreach (PlayerInfo player in OnlineManager.Instance.playerList)
            {
                Debug.Log("statistic`+");
                Debug.Log(player);
                Transform playerSingleTransform = Instantiate(playerSingleStats, container);
                PlayerSingleStat statPlayerSingleUI = playerSingleTransform.gameObject.GetComponent<PlayerSingleStat>();


                statPlayerSingleUI.SetId(player.lobbyPlayerId.ToSafeString());
                statPlayerSingleUI.playerNameText.text = player.name.ToSafeString();
                statPlayerSingleUI.team.text = player.team.ToSafeString();

                statPlayerSingleUI.characterImage.sprite = LobbyAssets.Instance.GetSprite(player.playerCharacter);



                statPlayerSingleUI.gameObject.SetActive(true);


                //LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.GetComponent<LobbyPlayerSingleUI>();

                /*lobbyPlayerSingleUI.SetKickPlayerButtonVisible(
                    LobbyManager.Instance.IsLobbyHost() &&
                    player.Id != AuthenticationService.Instance.PlayerId // Don't allow kick self
                );
                */

                // LobbyPlayers.Add(player.Id, lobbyPlayerSingleUI);
                //changeGameModeButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());
                // lobbyNameText.text = lobby.Name;
                //playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
                //gameModeText.text = lobby.Data[LobbyManager.KEY_GAME_MODE].Value;


                //Show();

                AddUserHandler(playerSingleTransform.gameObject.GetComponent<VivoxUserHandler>());

                player.PlayerSingleStat = statPlayerSingleUI;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    private void AddUserHandler(VivoxUserHandler playerLobbyHandler)
    {
        VivoxManager.Instance.m_vivoxUserHandlers.Add(playerLobbyHandler);
    }

    private void Hide()
    {
        container.gameObject.SetActive(false);
    }

    private void Show()
    {
        container.gameObject.SetActive(true);
    }



}
