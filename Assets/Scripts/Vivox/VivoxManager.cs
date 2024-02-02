using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Android;


public class VivoxManager : MonoBehaviour
{
    public List<VivoxUserHandler> m_vivoxUserHandlers;
    public VivoxSetup m_VivoxSetup = new VivoxSetup();

    public static VivoxManager Instance;
    // Start is called before the first frame update
    void Start()
    {
        //   Instance = this;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Awake()
    {
         //  StartVivoxJoin();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   public void StartVivoxLogin()
    {
        //if (m_VivoxSetup.m_loginSession != null)
        //  return;

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }       
#endif

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

    public void StartVivoxJoin()
    {
        m_VivoxSetup.JoinLobbyChannel(LobbyManager.Instance.joinedLobby.Id, OnVivoxJoinComplete);

        void OnVivoxJoinComplete(bool didSucceed)
        {
            if (!didSucceed)
            {
                Debug.Log(LobbyManager.Instance.joinedLobby.Id);
                Debug.LogError("Vivox connection failed! Retrying in 5s...");
                StartCoroutine(RetryConnection(StartVivoxJoin, LobbyManager.Instance.joinedLobby.Id));
            }
        }
    }

    public IEnumerator WaitForJoin()
    {
       yield return new WaitForSeconds(4f);
        StartVivoxJoin();
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
