using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;
using Unity.VisualScripting;

public class LobbyPlayerSingleUI : MonoBehaviour {


    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Button kickPlayerButton;

    [SerializeField] private TMP_Dropdown selectTeamDropdown;

    public string selected;
    private Player player;



    private void Awake() {
        //LobbyUI.Instance.dropDownExpanded = false;
        Debug.Log("awake");
        kickPlayerButton.onClick.AddListener(KickPlayer);
        selectTeamDropdown.onValueChanged.AddListener(delegate
        {
            SelectTeam(selectTeamDropdown);
        });

        selectTeamDropdown.gameObject.SetActive(true);
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        if (selectTeamDropdown.IsExpanded)
        {
            LobbyUI.Instance.dropDownExpanded = true;
         //   Debug.Log("aaaaaaaaa");
        }
        else
            LobbyUI.Instance.dropDownExpanded = false;
    }

    public void SetKickPlayerButtonVisible(bool visible) {
        kickPlayerButton.gameObject.SetActive(visible);
    }

    public void SetTeamClickable(bool visible)
    {
        //If not owner, disable Team selector
        selectTeamDropdown.enabled = visible;
        Debug.Log("ccccccccccc");
        //If owner, was it expanded?
        if (visible)
        {
            // selectTeamDropdown.Show();
            Debug.Log(LobbyUI.Instance.dropDownExpanded);
            if(LobbyUI.Instance.dropDownExpanded)
                //selectTeamDropdown.s
                selectTeamDropdown.Show();

        }
    }

    public void UpdatePlayer(Player player) {
        this.player = player;
        playerNameText.text = player.Data[LobbyManager.KEY_PLAYER_NAME].Value;
        LobbyManager.PlayerCharacter playerCharacter = 
            System.Enum.Parse<LobbyManager.PlayerCharacter>(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value);
        characterImage.sprite = LobbyAssets.Instance.GetSprite(playerCharacter);

        Debug.Log(",,,,," + player.Data[LobbyManager.KEY_PLAYER_TEAM].Value);
        selected = player.Data[LobbyManager.KEY_PLAYER_TEAM].Value;

        selectTeamDropdown.value = int.Parse(selected);
        //selectTeamDropdown.itemText.text = player.Data[LobbyManager.KEY_PLAYER_TEAM].Value;

    }

    private void KickPlayer() {
        if (player != null) {
            LobbyManager.Instance.KickPlayer(player.Id);
        }
    }

    private void SelectTeam(TMP_Dropdown change)
    {
        selectTeamDropdown.value = change.value;
        //selectTeamDropdown.itemText.text = " " + change.value;

        LobbyManager.Instance.ChangeTeam(player.Id, change.value.ToString());

        Debug.Log("CHANGE TO TEAM: " + change.value);
    }


}