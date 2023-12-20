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

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

        showStatsMobile.onClick.AddListener(() => {
            if (gamefinished)
            {
                ScrollBar.SetActive(true);
                container.gameObject.SetActive(true);

            }
            else
            {
                ScrollBar.SetActive(!container.gameObject.activeSelf);
                container.gameObject.SetActive(!container.gameObject.activeSelf);

            }
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
               // Debug.Log("////" +  player.lobbyPlayerId);

//                Debug.Log("////" + AuthenticationService.Instance.PlayerId);

                Debug.Log("////" + player.lobbyPlayerId.ToSafeString() == AuthenticationService.Instance.PlayerId);

                if (player.lobbyPlayerId.ToSafeString() == AuthenticationService.Instance.PlayerId)
                    statPlayerSingleUI.ChangeBackground();

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

        }
    }

    private void Show()
    {
        container.gameObject.SetActive(true);
        ScrollBar.SetActive(true);
       // Debug.Log("show");
    }

    public void FinishGame()
    {
        gamefinished = true;
        Show();
        container.transform.position = new Vector3(container.transform.position.x, container.transform.position.y - 50, 0);
        respawnMsg.SetActive(false);
    }

}
