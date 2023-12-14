using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerSingleStat : LobbyPlayerSingleUI 
{


    public int kills = 0;
    public bool isAlive = true;
    public TextMeshProUGUI killsDeaths;

   /* [SerializeField] public TextMeshProUGUI playerNameText;
    [SerializeField] public Image characterImage;
    [SerializeField] public Button kickPlayerButton;
    */[SerializeField] public TextMeshProUGUI team;

    /*public Sprite mutedSprite;
    public Sprite soundSprite;
    public GameObject mutedMic;
    public Sprite soundMic;

    public float soundValue;
    public bool isSounding = true;
    public bool IsLocalPlayer;
    [SerializeField] private GameObject sound;
    [SerializeField] private Scrollbar soundBar;
    [SerializeField] private Button soundButton;

    public string playerId;
    public VivoxUserHandler userHandler;
    */


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetId(string id)
    {
        playerId = id;
        userHandler.SetId(id);
    }

    public override void DisableVoice(bool shouldResetUi)
    {
        if (shouldResetUi)
        {
            soundBar.value = VivoxUserHandler.NormalizedVolumeDefault;
        }
        MuteUnMute(true);

    }


    //Para el botón
    public override void MuteUnMute(bool mute)
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


    public override void ChangeVolume(float soundVal)
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

}
