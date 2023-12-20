using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
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


    /*  [SerializeField]
      private GameObject joystickLeft;

      [SerializeField]
      private GameObject joystickRight;
    */
    void Start()
    {
        backToLobbyButton.onClick.AddListener(() =>
        {
            Debug.Log("BACK TO LOBBY");
            //Destroy(OnlineManager.Instance);
            //Reset all in-game variables
            Debug.Log(LobbyManager.Instance.joinedLobby.Id);
            OnlineManager.Instance.playersCreated = false;
            Time.timeScale = 1;
            SceneLoader.Load(SceneLoader.Scene.LobbyScene);

        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
    //Called only by server to server
    public void ShowEndGameServer(int winnerTeam)
    {
        ShowEndGameClientRpc(winnerTeam);
        Time.timeScale = 0;
    }

    [ClientRpc]
    public void ShowEndGameClientRpc(int winnerTeam) 
    {
        endGame.SetActive(true);
        winnerTeamText.text = "Team " + winnerTeam + "won the game!";
        statisticsUI.SetActive(true);
        // backToLobbyButton.gameObject.SetActive(true);
        backToLobby.SetActive(true);
        Reload.SetActive(false);
        statisticsUI.GetComponent<StatisticsUI>().FinishGame();
        scrollBar.SetActive(true);



        Time.timeScale = 0;
        mobileUI.SetActive(false);
#if UNITY_ANDROID
        mobileUI.SetActive(false);

       // joystickLeft.SetActive(false);
     //   joystickRight.SetActive(false);        

#endif
    }
}
