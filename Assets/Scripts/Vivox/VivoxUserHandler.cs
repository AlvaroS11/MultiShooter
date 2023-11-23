using UnityEngine;
using Unity.Services.Vivox;
using VivoxUnity;

/// <summary>
/// Listens for changes to Vivox state for one user in the lobby.
/// Instead of going through Relay, this will listen to the Vivox service since it will already transmit state changes for all clients.
/// </summary>
public class VivoxUserHandler : MonoBehaviour
{
    [SerializeField]
    public LobbyPlayerSingleUI lobbyPlayer;

    [SerializeField]
    private IChannelSession m_channelSession;
    [SerializeField]
    private string m_id;
    [SerializeField]
    private string m_vivoxId;

    private const int k_volumeMin = -50, k_volumeMax = 20; // From the Vivox docs, the valid range is [-50, 50] but anything above 25 risks being painfully loud.

    public static float NormalizedVolumeDefault
    {
        get { Debug.Log((0f - k_volumeMin) / (k_volumeMax - k_volumeMin)); return (0f - k_volumeMin) / (k_volumeMax - k_volumeMin); }
    }

    public void Start()
    {
        //   lobbyPlayer.DisableVoice(true);
        lobbyPlayer = GetComponent<LobbyPlayerSingleUI>();


        foreach (var device in Microphone.devices)
        {
            Debug.Log("Name: " + device);
        }
    }

    public void SetId(string id)
    {

        m_id = id;

        Account account = new Account(id);

        m_vivoxId = $"sip:.{account.Issuer}.{m_id}.{globalVariables.environmentId_dev}.@{account.Domain}";

        if (m_channelSession != null)
        {
            foreach (var participant in m_channelSession.Participants)
            {
                Debug.Log(m_id == participant.Account.DisplayName);
                //Al volver a entrar no sse pone el microfono
                Debug.Log(participant.Account.DisplayName);
                Debug.Log(m_id);
                if (m_id == participant.Account.DisplayName)
                {
                    m_vivoxId = participant.Key;
                    lobbyPlayer.IsLocalPlayer = participant.IsSelf;
                    lobbyPlayer.MuteUnMute(true);
                    break;
                }
            }
        }
    }

    public void OnChannelJoined(IChannelSession channelSession) // Called after a connection is established, which begins once a lobby is joined.
    {
        //Check if we are muted or not

        m_channelSession = channelSession;
        m_channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
        m_channelSession.Participants.BeforeKeyRemoved += BeforeParticipantRemoved;
        m_channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;

        Debug.Log("^^^^");
        Debug.Log(channelSession == null);
        Debug.Log(channelSession.Participants);

        //NO ESTÁ LLEGANDO, PARTICIPANTS ES 0?
        /*foreach (var participant in m_channelSession.Participants)
        {
            Debug.Log("**");
            Debug.Log(m_id);
            Debug.Log(participant.Account.DisplayName);
            if (m_id == participant.Account.DisplayName)
            {
//                m_vivoxId = participant.Key;
                lobbyPlayer.IsLocalPlayer = participant.IsSelf;
                lobbyPlayer.MuteUnMute(true);
                break;
            }
        }*/
    }

    public void OnChannelLeft() // Called when we leave the lobby.
    {
        if (m_channelSession != null) // It's possible we'll attempt to leave a channel that isn't joined, if we leave the lobby while Vivox is connecting.
        {
            m_channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
            m_channelSession.Participants.BeforeKeyRemoved -= BeforeParticipantRemoved;
            m_channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
            m_channelSession = null;
        }
    }

    /// <summary>
    /// To be called whenever a new Participant is added to the channel, using the events from Vivox's custom dictionary.
    /// </summary>
    private void OnParticipantAdded(object sender, KeyEventArg<string> keyEventArg)
    {
        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        var participant = source[keyEventArg.Key];
        var username = participant.Account.DisplayName;

        bool isThisUser = username == m_id;
        Debug.Log(username);
        Debug.Log(m_id);
        if (isThisUser)
        {
            m_vivoxId = keyEventArg.Key; // Since we couldn't construct the Vivox ID earlier, retrieve it here.
            lobbyPlayer.IsLocalPlayer = participant.IsSelf;

            Debug.Log("*****");
            Debug.Log(participant.IsSelf);
            Debug.Log(lobbyPlayer.IsLocalPlayer);
            Debug.Log(lobbyPlayer.gameObject.name);
            if (lobbyPlayer == null)
            {
                Debug.Log("WAS NULL!");
                lobbyPlayer = GetComponent<LobbyPlayerSingleUI>();

            }
            Debug.Log(lobbyPlayer.gameObject.name);
            if (!participant.IsMutedForAll)
                lobbyPlayer.ChangeVolume(0);

            else
                lobbyPlayer.ChangeVolume(0.5f);
        }
        else
        {
            if (!participant.LocalMute)
                lobbyPlayer.ChangeVolume(0.5f);

            else
                lobbyPlayer.ChangeVolume(0);

        }
    }

    private void BeforeParticipantRemoved(object sender, KeyEventArg<string> keyEventArg)
    {
        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        var participant = source[keyEventArg.Key];
        var username = participant.Account.DisplayName;

        bool isThisUser = username == m_id;
        if (isThisUser)
        {
            lobbyPlayer.DisableVoice(true);
        }
    }

    private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> valueEventArg)
    {
        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        var participant = source[valueEventArg.Key];
        var username = participant.Account.DisplayName;
        string property = valueEventArg.PropertyName;


        if (username == m_id)
        {
            if (property == "UnavailableCaptureDevice")
            {
                if (participant.UnavailableCaptureDevice)
                {
                    lobbyPlayer.MuteUnMute(false);
                    participant.SetIsMuteForAll(true, null);
                }
                else
                {
                    lobbyPlayer.MuteUnMute(false);

                    participant.SetIsMuteForAll(false, null); //  This call is asynchronous, so it's possible to exit the lobby before this completes, resulting in a Vivox error.
                }
            }
            else if (property == "IsMutedForAll")
            {
                if (participant.IsMutedForAll)
                    lobbyPlayer.MuteUnMute(false);
                else
                    lobbyPlayer.MuteUnMute(true);
            }
        }
    }

    public void OnVolumeSlide(float volumeNormalized)
    {
        if (m_channelSession == null || m_vivoxId == null) // Verify initialization, since SetId and OnChannelJoined are called at different times for local vs. remote clients.
        {
            if (m_channelSession == null)
            {
                OnChannelJoined(VivoxManager.Instance.m_VivoxSetup.GetChannel());
            }
            return;

        }


        int vol = (int)Mathf.Clamp(k_volumeMin + (k_volumeMax - k_volumeMin) * volumeNormalized, k_volumeMin, k_volumeMax); // Clamping as a precaution; if UserVolume somehow got above 1, listeners could be harmed.
        bool isSelf = m_channelSession.Participants[m_vivoxId].IsSelf;

        if (volumeNormalized == 0)
        {
            OnMuteToggle(true);
            return;
        }
        else if (VivoxService.Instance.Client.AudioInputDevices.Muted)
        {
            OnMuteToggle(false);
        }

        if (isSelf)
        {
            VivoxService.Instance.Client.AudioInputDevices.VolumeAdjustment = vol;
        }
        else
        {
            m_channelSession.Participants[m_vivoxId].LocalVolumeAdjustment = vol;
            Debug.Log(m_channelSession.Participants[m_vivoxId].LocalVolumeAdjustment);
        }
    }

    public void OnMuteToggle(bool isMuted)
    {
        if (m_channelSession == null || m_vivoxId == null)
            return;

        bool isSelf = m_channelSession.Participants[m_vivoxId].IsSelf;
        if (isSelf)
        {
            VivoxService.Instance.Client.AudioInputDevices.Muted = isMuted;
        }
        else
        {
            m_channelSession.Participants[m_vivoxId].LocalMute = isMuted;
        }
    }
}
