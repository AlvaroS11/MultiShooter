using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;
using Unity.VisualScripting;
using Unity.Services.Authentication;
using Unity.Netcode;
using Unity.Collections;

public class LobbyPlayerSingleUI : MonoBehaviour {


    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Button kickPlayerButton;

    [SerializeField] private TMP_Dropdown selectTeamDropdown;

    public string selected;
    [SerializeField]
    public Player player;

    public string playerId;


    private void Awake() {
        kickPlayerButton.onClick.AddListener(KickPlayer);



       selectTeamDropdown.onValueChanged.AddListener(delegate
        {
            SelectTeam(selectTeamDropdown);
        });
      

        //selectTeamDropdown.gameObject.SetActive(true);
    }

    private void Start()
    {
      

    }

    private void Update()
    {
        if (selectTeamDropdown.IsExpanded)
        {
            LobbyUI.Instance.dropDownExpanded = true;
        }
        else
            LobbyUI.Instance.dropDownExpanded = false;
    }

    public void SetKickPlayerButtonVisible(bool visible) {
        kickPlayerButton.gameObject.SetActive(visible);
    }

    public void SetUpTemplate(int team, string name, LobbyManager.PlayerCharacter playerCharacter)
    {
        UpdateTeamUi(team);
        UpdateCharacterUI(playerCharacter);
        UpdateNameUI(name);
    }

    public void SetTeamClickable(bool visible)
    {
        //If not owner, disable Team selector
        selectTeamDropdown.enabled = visible;
    }

    //HACER CON NETCODE
    public void UpdatePlayer(Player player) {
        this.player = player;
        playerNameText.text = player.Data[LobbyManager.KEY_PLAYER_NAME].Value;
        LobbyManager.PlayerCharacter playerCharacter = 
            System.Enum.Parse<LobbyManager.PlayerCharacter>(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value);
        characterImage.sprite = LobbyAssets.Instance.GetSprite(playerCharacter);

        selected = player.Data[LobbyManager.KEY_PLAYER_TEAM].Value;

        selectTeamDropdown.value = int.Parse(selected);
        //selectTeamDropdown.itemText.text = player.Data[LobbyManager.KEY_PLAYER_TEAM].Value;

    }


    public void UpdateTeamUi(int team)
    {
        selectTeamDropdown.value = team - 1;
    }

    public void UpdateCharacterUI(LobbyManager.PlayerCharacter playerCharacter)
    {

        //PARA EL NOMBRE:         playerNameText.text = player.Data[LobbyManager.KEY_PLAYER_NAME].Value;

        characterImage.sprite = LobbyAssets.Instance.GetSprite(playerCharacter);

    }

    public void UpdateNameUI(FixedString128Bytes name)
    {

        //PARA EL NOMBRE:         playerNameText.text = player.Data[LobbyManager.KEY_PLAYER_NAME].Value;

        playerNameText.text = name.ToSafeString();

    }

    private void KickPlayer() {
        if (player != null) {
            LobbyManager.Instance.KickPlayer(player.Id);
        }
    }

    public bool isSelf()
    {
        Debug.Log(playerId);
        return playerId == AuthenticationService.Instance.PlayerId;
    }

    public void SelectTeam(TMP_Dropdown change)
    {
        selectTeamDropdown.value = change.value;
        int prevTeam = LobbyManager.Instance.GetTeam(playerId);
        OnlineManager.Instance.ChangeTeamServerRpc(playerId, change.value + 1, NetworkManager.Singleton.LocalClientId);
    }


}