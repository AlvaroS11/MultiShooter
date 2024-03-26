using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Authentication;



public class StatisticsUI : NetworkBehaviour
{

    public static StatisticsUI Instance { get; private set; }

    [SerializeField] private Transform playerSingleStats;

    [SerializeField] private Transform container;

    [SerializeField] private GameObject ScrollBar;


    [SerializeField] private Button showStatsMobile;

    public bool gamefinished = false;

    public GameObject respawnMsg;

    public GameObject menu;

    public GameObject reloadBar;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

        showStatsMobile.onClick.AddListener(() => {
            if (gamefinished)
            {
                ScrollBar.SetActive(true);
                container.gameObject.SetActive(true);
                menu.SetActive(true);
                reloadBar.SetActive(false);
            }
            else
            {
                ScrollBar.SetActive(!container.gameObject.activeSelf);
                menu.SetActive(!menu.activeSelf);
                container.gameObject.SetActive(!container.gameObject.activeSelf);
                reloadBar.SetActive(!container.gameObject.activeSelf);
            }
        });

        if (LobbyManager.Instance.m_gameMode == LobbyManager.GameMode.Free_for_all)
        {
            GameAssets.Instance.teamsText.SetActive(false);
        }
        else
            GameAssets.Instance.teamsText.SetActive(true);

#if UNITY_STANDALONE_WIN

            showStatsMobile.gameObject.SetActive(false);

#endif

        Hide();
    }

    void Update()
    {
#if UNITY_STANDALONE_WIN
        if (Input.GetKey(KeyCode.Tab) || (Input.GetKey(KeyCode.Escape)))
        {
            Show();
        }
        else
            Hide();

#elif UNITY_ANDROID || UNITY_IOS
        //add button to show/hide stats

#endif

    }


    [ClientRpc]
    public void InitializeStatisticsClientRpc()
    {
        try
        {
            foreach (PlayerInfo player in OnlineManager.Instance.playerList)
            {
                Transform playerSingleTransform = Instantiate(playerSingleStats, container);
                PlayerSingleStat statPlayerSingleUI = playerSingleTransform.gameObject.GetComponent<PlayerSingleStat>();


                statPlayerSingleUI.SetId(player.lobbyPlayerId.ToSafeString());
                statPlayerSingleUI.playerNameText.text = player.name.ToSafeString();

                if (LobbyManager.Instance.m_gameMode == LobbyManager.GameMode.Free_for_all)
                {
                    statPlayerSingleUI.team.gameObject.active = false;
                    statPlayerSingleUI.teamImage.gameObject.SetActive(false);
                }
                else
                    statPlayerSingleUI.teamImage.sprite = statPlayerSingleUI.teamImages[--player.team];
                    //statPlayerSingleUI.team.text = player.team.ToSafeString();

                statPlayerSingleUI.characterImage.sprite = GameAssets.Instance.GetSprite(player.playerCharacter);



                statPlayerSingleUI.gameObject.SetActive(true);
 
                if (player.lobbyPlayerId.ToSafeString() == AuthenticationService.Instance.PlayerId)
                    statPlayerSingleUI.ChangeBackground();

                statPlayerSingleUI.soundBar.value = player.PlayerVolume;
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
        if (!gamefinished)
        {
            container.gameObject.SetActive(false);
            ScrollBar.SetActive(false);
            menu.SetActive(false);
            reloadBar.SetActive(true);
        }
    }

    private void Show()
    {
        container.gameObject.SetActive(true);
        ScrollBar.SetActive(true);
        menu.SetActive(true);
        reloadBar.SetActive(false);
    }

    public void FinishGame()
    {
        gamefinished = true;
        Show();
        container.transform.position = new Vector3(container.transform.position.x, container.transform.position.y - 50, 0);
        respawnMsg.SetActive(false);
    }

}
