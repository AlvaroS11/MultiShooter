﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using VivoxUnity;

/// <summary>
/// Handles setting up a voice channel once inside a lobby.
/// </summary>
public class VivoxSetup
{
    private bool m_hasInitialized = false;
    private bool m_isMidInitialize = false;
    public ILoginSession m_loginSession = null;
    private IChannelSession m_channelSession = null;
    public List<VivoxUserHandler> m_userHandlers;

    /// <summary>
    /// Initialize the Vivox service, before actually joining any audio channels.
    /// </summary>
    /// <param name="onComplete">Called whether the login succeeds or not.</param>
    public void Initialize(List<VivoxUserHandler> userHandlers, Action<bool> onComplete)
    {
        if (m_isMidInitialize)
            return;

        if (m_loginSession != null)
        {
            UnityEngine.Debug.Log(m_loginSession.State);
            if (m_loginSession.State == LoginState.LoggedIn)
            {
                onComplete?.Invoke(true);
                return;
            }

        }
        UnityEngine.Debug.Log("ISERROR IN INITIALIZE");

        m_isMidInitialize = true;

        m_userHandlers = userHandlers;
        VivoxService.Instance.Initialize();
        Account account = new Account(AuthenticationService.Instance.PlayerId);
        m_loginSession = VivoxService.Instance.Client.GetLoginSession(account);
        string token = m_loginSession.GetLoginToken();

        m_loginSession.BeginLogin(token, SubscriptionMode.Accept, null, null, null, result =>
        {
            try
            {
                m_loginSession.EndLogin(result);
                m_hasInitialized = true;
                onComplete?.Invoke(true);
            }
            catch (Exception ex)
            {

                UnityEngine.Debug.LogWarning("Vivox failed to login: " + ex.Message);
                UnityEngine.Debug.Log(m_loginSession.State);
                onComplete?.Invoke(false);
            }
            finally
            {
                m_isMidInitialize = false;
            }
        });
    }

    /// <summary>
    /// Once in a lobby, start joining a voice channel for that lobby. Be sure to complete Initialize first.
    /// </summary>
    /// <param name="onComplete">Called whether the channel is successfully joined or not.</param>
    public void JoinLobbyChannel(string lobbyId, Action<bool> onComplete)
    {
        UnityEngine.Debug.Log("ISERROR IN JOINLOBBY");

        if (!m_hasInitialized || m_loginSession.State != LoginState.LoggedIn)
        {
            UnityEngine.Debug.LogWarning("Can't join a Vivox audio channel, as Vivox login hasn't completed yet.");
            onComplete?.Invoke(false);
            return;
        }

        UnityEngine.Debug.Log("ISERROR IN JOINLOBBY");

        ChannelType channelType = ChannelType.NonPositional;
        Channel channel = new Channel(lobbyId + "_voice", channelType, null);
        m_channelSession = m_loginSession.GetChannelSession(channel);
        string token = m_channelSession.GetConnectToken();

        m_channelSession.BeginConnect(true, false, true, token, result =>
        {
            try
            {
                // Special case: It's possible for the player to leave the lobby between the time we called BeginConnect and the time we hit this callback.
                // If that's the case, we should abort the rest of the connection process.
                if (m_channelSession.ChannelState == ConnectionState.Disconnecting ||
                    m_channelSession.ChannelState == ConnectionState.Disconnected)
                {
                    UnityEngine.Debug.LogWarning(
                        "Vivox channel is already disconnecting. Terminating the channel connect sequence.");
                    HandleEarlyDisconnect();
                    return;
                }

                m_channelSession.EndConnect(result);
                onComplete?.Invoke(true);
                UnityEngine.Debug.Log(m_userHandlers.Count);
                if(m_userHandlers.Count == 0)
                {
                    m_userHandlers = VivoxManager.Instance.m_vivoxUserHandlers;
                }

                foreach (VivoxUserHandler userHandler in m_userHandlers)
                    userHandler.OnChannelJoined(m_channelSession);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("Vivox failed to connect: " + ex.Message);
                onComplete?.Invoke(false);
                m_channelSession?.Disconnect();
            }
        });
    }

    public IChannelSession GetChannel()
    {
        return m_channelSession;
    }

    /// <summary>
    /// To be called when leaving a lobby.
    /// </summary>
    public void LeaveLobbyChannel()
    {
        UnityEngine.Debug.Log("LEAVE LOBBY CHANNEL");
        if (m_channelSession != null)
        {
            // Special case: The EndConnect call requires a little bit of time before the connection actually completes, but the player might
            // disconnect before then. If so, sending the Disconnect now will fail, and the played would stay connected to voice while no longer
            // in the lobby. So, wait until the connection is completed before disconnecting in that case.
            if (m_channelSession.ChannelState == ConnectionState.Connecting)
            {
                UnityEngine.Debug.LogWarning(
                    "Vivox channel is trying to disconnect while trying to complete its connection. Will wait until connection completes.");
                HandleEarlyDisconnect();
                return;
            }

            ChannelId id = m_channelSession.Channel;
            m_channelSession?.Disconnect(
                (result) =>
                {
                    m_loginSession.DeleteChannelSession(id);
                    m_channelSession = null;
                });

            UnityEngine.Debug.Log("DISCONECCTING LOBBY CHANNEL");
            //UnityEngine.MonoBehaviour.Destroy(UnityEngine.GameObject.Find("VivoxUnity.VxUnityInterop (Singleton)"));
            // m_loginSession.Logout();
            // UnityEngine.D UnityEngine.GameObject.Find("VivoxUnity.VxUnityInterop (Singleton)")

        }

        foreach (VivoxUserHandler userHandler in m_userHandlers)
            userHandler.OnChannelLeft();
    }

    private void HandleEarlyDisconnect()
    {
        DisconnectOnceConnected();
    }

    async void DisconnectOnceConnected()
    {
        while (m_channelSession?.ChannelState == ConnectionState.Connecting)
        {
            await Task.Delay(200);
            return;
        }

        LeaveLobbyChannel();
    }

    /// <summary>
    /// To be called on quit, this will disconnect the player from Vivox entirely instead of just leaving any open lobby channels.
    /// </summary>
    public void Uninitialize()
    {
        if (!m_hasInitialized)
            return;
        m_loginSession.Logout();
    }
}