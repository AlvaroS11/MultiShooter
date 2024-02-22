using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EndGame : NetworkBehaviour
{
    // Start is called before the first frame update

    [SerializeField]
    private TextMeshProUGUI winnerTeamText;


    [SerializeField]
    private Button backToLobbyButton;

    [SerializeField]
    private GameObject backToLobby;
    //private TextMeshProUGUI backButton;

    [SerializeField]
    private GameObject endGame;

    [SerializeField]
    private GameObject statisticsUI;

    [SerializeField]
    private GameObject mobileUI;


    [SerializeField]
    private GameObject Reload;

    [SerializeField]
    private GameObject scrollBar;


    void Start()
    {
        backToLobbyButton.onClick.AddListener(() =>
        {
            //Reset all in-game variables
            // Debug.Log(LobbyManager.Instance.joinedLobby.Id);
            Debug.Log("Leaving to lobby");
            OnlineManager.Instance.playersCreated = false;
            try
            {
                PlayerManager disconnectedPlayer = OnlineManager.Instance.playerList.Find(x => x.playerObject.GetComponent<PlayerManager>().isOwnPlayer == true).playerObject.GetComponent<PlayerManager>();
                disconnectedPlayer.DisconnectPlayerServerRpc(disconnectedPlayer.clientId);
            }
            catch
            {
                Debug.LogError("not found");

            }
            finally
            {
                OnlineManager.Instance.playerList.Clear();
                Time.timeScale = 1;
                SceneLoader.Load(SceneLoader.Scene.LobbyScene);
            }

        });
    }

    void Update()
    {
        
    }

    
    //Called only by server to server
    public void ShowEndGameServer(int winnerTeam, string winnerName)
    {
        if (LobbyManager.Instance.m_gameMode == LobbyManager.GameMode.Team_DeathMatch)
        {
            ShowEndGameTeamClientRpc(winnerTeam);
        }
        else
        {
            ShowEndGameFreeClientRpc(winnerName);
        }

        Time.timeScale = 0;
        mobileUI.SetActive(false);
#if UNITY_ANDROID || UNITY_IOS
        mobileUI.SetActive(false);    

#endif
    }

    [ClientRpc]
    public void ShowEndGameFreeClientRpc(string winnerUser)
    {
        endGame.SetActive(true);
        winnerTeamText.text = "Player " + winnerUser + " won the game!";
        statisticsUI.SetActive(true);
        backToLobby.SetActive(true);
        Reload.SetActive(false);
        statisticsUI.GetComponent<StatisticsUI>().FinishGame();
        scrollBar.SetActive(true);
    }

    [ClientRpc]
    public void ShowEndGameTeamClientRpc(int winnerTeam) 
    {
        endGame.SetActive(true);
        winnerTeamText.text = "Team " + winnerTeam + " won the game!";
        statisticsUI.SetActive(true);
        backToLobby.SetActive(true);
        Reload.SetActive(false);
        statisticsUI.GetComponent<StatisticsUI>().FinishGame();
        scrollBar.SetActive(true);
    }
}
