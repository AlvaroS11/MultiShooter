using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;


public class StatisticsUI : NetworkBehaviour
{

    public static StatisticsUI Instance { get; private set; }

    [SerializeField] private Transform playerSingleStats;


    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
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
                Transform playerSingleTransform = Instantiate(playerSingleStats, transform);
                PlayerSingleStat statPlayerSingleUI = playerSingleTransform.gameObject.GetComponent<PlayerSingleStat>();


                statPlayerSingleUI.SetId(player.lobbyPlayerId.ToSafeString());
                statPlayerSingleUI.playerNameText.text = player.name.ToSafeString();
                statPlayerSingleUI.team.text = player.team.ToSafeString();



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



}
