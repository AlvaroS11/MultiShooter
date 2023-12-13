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


    [SerializeField] public TextMeshProUGUI playerNameText;
    [SerializeField] public Image characterImage;
    [SerializeField] public Button kickPlayerButton;

    [SerializeField] private TMP_Dropdown selectTeamDropdown;

    [SerializeField] public GameObject sound;
    [SerializeField] public Scrollbar soundBar;
    [SerializeField] public Button soundButton;


    public string selected;
    [SerializeField]
    public Player player;

    public string playerId;
    public ulong playerIdRelay;

    public Sprite mutedSprite;
    public Sprite soundSprite;
    public GameObject mutedMic;
    public Sprite soundMic;

    public float soundValue;
    public bool isSounding = true;


    public bool IsLocalPlayer;

    public VivoxUserHandler userHandler;




    private void Awake() {
        kickPlayerButton.onClick.AddListener(KickPlayer);


        if (selectTeamDropdown != null)
        {
            selectTeamDropdown.onValueChanged.AddListener(delegate
             {
                 SelectTeam(selectTeamDropdown);
             });
        }

        soundBar.onValueChanged.AddListener((float val) =>
        {
            ChangeVolume(val);
        });

        soundButton.onClick.AddListener(() =>
        {
            MuteUnMute(!isSounding);
        });


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

    public virtual void SetId(string id)
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

    public virtual void DisableVoice(bool shouldResetUi)
    {
        if (shouldResetUi)
        {
            soundBar.value = VivoxUserHandler.NormalizedVolumeDefault;
        }
        MuteUnMute(true);

    }
    

    //Para el botón
    public virtual void MuteUnMute(bool mute)
    {
        isSounding = mute;


        Debug.Log("muteUnMute");
        if (isSounding)
        {
            soundValue = .5f;
            soundBar.value = .5f;
        }
        else
        {
            soundValue = 0;
            soundBar.value = 0;
        }
        ChangeVolume(soundValue);
    }


    public virtual void ChangeVolume(float soundVal)
    {
        //Debug.Log("changing sound " + playerId + soundVal);
        soundValue = soundVal;
        if (soundVal == 0)
        {
            if (!IsLocalPlayer)
            {
                //Debug.Log("*");
                soundButton.image.sprite = mutedSprite;
            }
            else
            {
                Debug.Log("ADSA");

                mutedMic.SetActive(true);
                soundButton.image.sprite = soundMic;
            }
        }
        else
        {
        if (!IsLocalPlayer)
            {
                //Debug.Log("*");

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


    public void UpdateTeamUi(int team)
    {
        selectTeamDropdown.value = team - 1;
    }

    public void UpdateCharacterUI(LobbyManager.PlayerCharacter playerCharacter)
    {
        characterImage.sprite = LobbyAssets.Instance.GetSprite(playerCharacter);
    }

    public void UpdateNameUI(FixedString128Bytes name)
    {
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