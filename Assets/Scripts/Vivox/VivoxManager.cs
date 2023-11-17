using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class VivoxManager : MonoBehaviour
{

    List<VivoxUserHandler> m_vivoxUserHandlers;
    VivoxSetup m_VivoxSetup = new VivoxSetup();

    public static VivoxManager Instance;
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    private void Awake()
    {
            //StartVivoxJoin();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   public void StartVivoxLogin()
    {
        m_VivoxSetup.Initialize(m_vivoxUserHandlers, OnVivoxLoginComplete);

        void OnVivoxLoginComplete(bool didSucceed)
        {
            if (!didSucceed)
            {
                Debug.LogError("Vivox login failed! Retrying in 5s...");
                StartCoroutine(RetryConnection(StartVivoxLogin, LobbyManager.Instance.joinedLobby.Id));
            }
        }
    }

    void StartVivoxJoin()
    {
        m_VivoxSetup.JoinLobbyChannel(LobbyManager.Instance.joinedLobby.Id, OnVivoxJoinComplete);

        void OnVivoxJoinComplete(bool didSucceed)
        {
            if (!didSucceed)
            {
                Debug.LogError("Vivox connection failed! Retrying in 5s...");
                StartCoroutine(RetryConnection(StartVivoxJoin, LobbyManager.Instance.joinedLobby.Id));
            }
        }
    }

    IEnumerator RetryConnection(Action doConnection, string lobbyId)
    {
        yield return new WaitForSeconds(5);
        if (LobbyManager.Instance != null && LobbyManager.Instance.joinedLobby.Id == lobbyId && !string.IsNullOrEmpty(lobbyId)
           ) // Ensure we didn't leave the lobby during this waiting period.
            doConnection?.Invoke();
    }

    public void LeaveVivox()
    {
        m_VivoxSetup.LeaveLobbyChannel();
    }
}
