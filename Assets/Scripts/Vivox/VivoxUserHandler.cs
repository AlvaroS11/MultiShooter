﻿using UnityEngine;
using Unity.Services.Vivox;
using VivoxUnity;
using UnityEngine.Audio;

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
        get { return (0f - k_volumeMin) / (k_volumeMax - k_volumeMin); }
    }

    public void Start()
    {
        lobbyPlayer = GetComponent<LobbyPlayerSingleUI>();
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


    private void OnParticipantAdded(object sender, KeyEventArg<string> keyEventArg)
    {
        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        var participant = source[keyEventArg.Key];
        var username = participant.Account.DisplayName;

        bool isThisUser = username == m_id;

        try
        {
            if (isThisUser)
            {
                m_vivoxId = keyEventArg.Key;
                lobbyPlayer.IsLocalPlayer = participant.IsSelf;

                if (lobbyPlayer == null)
                {
                    lobbyPlayer = GetComponent<LobbyPlayerSingleUI>();

                }
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

            GetComponent<LobbyPlayerSingleUI>().soundBar.interactable = true;
            //VivoxManager.Instance.
        }
        catch(VivoxApiException e)
        {
            Debug.LogError("Vivox error");
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
        try
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

            // float vol2 = (float)Mathf.Log10(volumeNormalized)*20;

            if (volumeNormalized <= 0)
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
            }
        }catch(VivoxApiException e)
        {
            Debug.LogError(e);
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
