using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour
{


    public static LobbyCreateUI Instance { get; private set; }


    [SerializeField] private Button createButton;
    [SerializeField] private Button lobbyNameButton;
    [SerializeField] private Button publicPrivateButton;
    [SerializeField] private Button maxPlayersButton;
    [SerializeField] private Button gameModeButton;
    [SerializeField] private Button maxKillsButton;

    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI publicPrivateText;
    [SerializeField] private TextMeshProUGUI maxPlayersText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private TextMeshProUGUI maxKillsText;



    private string lobbyName;
    private bool isPrivate;
    private int maxPlayers;
    private int maxKills;

    private LobbyManager.GameMode gameMode;

    private void Awake()
    {
        Instance = this;

        createButton.onClick.AddListener(() => {
            LobbyManager.Instance.CreateLobby(
                lobbyName,
                maxPlayers,
                isPrivate,
                gameMode,
                maxKills
            );
            Hide();
        });

        lobbyNameButton.onClick.AddListener(() => {
            UI_InputWindow.Show_Static("Lobby Name", lobbyName, "abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ .,-", 20,
            () => {
                // Cancel
            },
            (string lobbyName) => {
                this.lobbyName = lobbyName;
                UpdateText();
            });
        });

        publicPrivateButton.onClick.AddListener(() => {
            isPrivate = !isPrivate;
            UpdateText();
        });

        maxPlayersButton.onClick.AddListener(() => {
            UI_InputWindow.Show_Static("Max Players", maxPlayers,
            () => {
                // Cancel
            },
            (int maxPlayers) => {
                this.maxPlayers = maxPlayers;
                UpdateText();
            });
        });

        gameModeButton.onClick.AddListener(() => {
            switch (gameMode)
            {
                default:
               case LobbyManager.GameMode.Team_DeathMatch:
                    gameMode = LobbyManager.GameMode.Free_for_all;
                    break;
                case LobbyManager.GameMode.Free_for_all:
                    gameMode = LobbyManager.GameMode.Team_DeathMatch;
                    break;
            }
            LobbyManager.Instance.m_gameMode = gameMode;
            UpdateText();
        });

        maxKillsButton.onClick.AddListener(() =>
        {
            UI_InputWindow.Show_Static("Max kills", maxKills,
            () => {
                // Cancel
            },
            (int maxKills) => {
                this.maxKills = maxKills;
                UpdateText();
            });

        });

        Hide();
    }

    private void UpdateText()
    {
        lobbyNameText.text = lobbyName;
        publicPrivateText.text = isPrivate ? "Private" : "Public";
        maxPlayersText.text = maxPlayers.ToString();
        maxKillsText.text = maxKills.ToString();
        //maxPlayersText.text = "Max Players: " + maxPlayers.ToString();
        //maxKillsText.text = "Max Kills: " + maxKills.ToString();
        gameModeText.text = gameMode.ToString().Replace("_", " ");
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        lobbyName = "MyLobby";
        isPrivate = false;
        maxPlayers = 4;
        maxKills = 3;
        //gameMode = LobbyManager.GameMode.Team_DeathMatch;
        gameMode = LobbyManager.Instance.m_gameMode;
        UpdateText();
    }

}