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

    [SerializeField] private GameObject sound;
    [SerializeField] private Scrollbar soundBar;
    [SerializeField] private Button soundButton;


    public string selected;
    [SerializeField]
    public Player player;

    public string playerId;

    public Sprite mutedSprite;
    public Sprite soundSprite;
    public GameObject mutedMic;
    public Sprite soundMic;

    public float soundValue;
    public bool isSounding = true;


    public bool IsLocalPlayer { private get; set; }

    public VivoxUserHandler userHandler;




    private void Awake() {
        kickPlayerButton.onClick.AddListener(KickPlayer);



       selectTeamDropdown.onValueChanged.AddListener(delegate
        {
            SelectTeam(selectTeamDropdown);
        });

        soundBar.onValueChanged.AddListener((float val) =>
        {
            ChangeVolume(val);
        });

        soundButton.onClick.AddListener(() =>
        {
            MuteUnMute(!isSounding);
        });


        //selectTeamDropdown.gameObject.SetActive(true);

        //EnableVoice(true);
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

    public void SetId(string id)
    {
        playerId = id;
        userHandler.SetId(id);
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

    public void DisableVoice(bool shouldResetUi)
    {
        if (shouldResetUi)
        {
            soundBar.value = VivoxUserHandler.NormalizedVolumeDefault;
        }
        MuteUnMute(true);

    }
    

    //Para el bot�n
    public void MuteUnMute(bool mute)
    {
        isSounding = mute;


        Debug.Log("muteUnMute");
        if (isSounding)
        {
           /* if (!IsLocalPlayer)
            {
                soundButton.image.sprite = soundSprite;
            }
            else
            {
                mutedMic.SetActive(true);
                soundButton.image.sprite = soundMic;
            }*/
            soundValue = .5f;
            soundBar.value = .5f;
        }
        else
        {
           /* if (!IsLocalPlayer)
            {
                soundButton.image.sprite = mutedSprite;
            }
            else
            {
                mutedMic.SetActive(false);
                soundButton.image.sprite = soundMic;
            }*/
            soundValue = 0;
            soundBar.value = 0;
        }
        //userHandler.OnVolumeSlide(soundValue);
        ChangeVolume(soundValue);
    }


    public void ChangeVolume(float soundVal)
    {
        //Debug.Log("changing sound " + playerId + soundVal);
        soundValue = soundVal;
        if (soundVal == 0)
        {
            if (!IsLocalPlayer)
            {
                soundButton.image.sprite = mutedSprite;
            }
            else
            {
                mutedMic.SetActive(true);
                soundButton.image.sprite = soundMic;
            }
        }
        else
        {
        if (!IsLocalPlayer)
        {
            soundButton.image.sprite = soundSprite;

        }
        else
        {
            mutedMic.SetActive(false);
            soundButton.image.sprite = soundMic;
        }
        mutedMic.SetActive(false);
    }

        userHandler.OnVolumeSlide(soundValue);

    }

    private void HandleVolume()
    {

    }

    /*public void EnableVoice(bool shouldResetUi)
    {
        /*if (shouldResetUi)
        {
            soundBar.value = VivoxUserHandler.NormalizedVolumeDefault;
            soundButton.image.sprite = soundSprite;
        }*/

    /*Debug.Log("IS LOCAL " + IsLocalPlayer);

    if (IsLocalPlayer)
    {
        //A�ADIR MICROFONO PARA SI ES EL MISMO JUGADOR

        mutedMic.SetActive(true);
        soundButton.image.sprite = soundMic; 
       /* m_volumeSliderContainer.Hide(0);
        m_muteToggleContainer.Show();
        m_muteIcon.SetActive(false);
        m_micMuteIcon.SetActive(true);
       */
    /* }
     else
     {
         mutedMic.SetActive(false);
         soundBar.value = VivoxUserHandler.NormalizedVolumeDefault;
         soundButton.image.sprite = soundSprite;
         /* m_volumeSliderContainer.Show();
          m_muteToggleContainer.Show();
          m_muteIcon.SetActive(true);
          m_micMuteIcon.SetActive(false);
         */
    /* }
 }*/


    /*    public void UpdatePlayer(Player player) {

            this.player = player;
            playerNameText.text = player.Data[LobbyManager.KEY_PLAYER_NAME].Value;
            LobbyManager.PlayerCharacter playerCharacter = 
                System.Enum.Parse<LobbyManager.PlayerCharacter>(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value);
            characterImage.sprite = LobbyAssets.Instance.GetSprite(playerCharacter);

            selected = player.Data[LobbyManager.KEY_PLAYER_TEAM].Value;

            selectTeamDropdown.value = int.Parse(selected);
            //selectTeamDropdown.itemText.text = player.Data[LobbyManager.KEY_PLAYER_TEAM].Value;

        }
    */


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
        if (playerId != null) {
            LobbyManager.Instance.KickPlayer(playerId);
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

   /* public void DesactivateSound()
    {
        sound.SetActive(false);
    }*/


}