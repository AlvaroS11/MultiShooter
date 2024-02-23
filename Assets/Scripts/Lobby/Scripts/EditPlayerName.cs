using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Text;
using UnityEngine.Windows;

public class EditPlayerName : MonoBehaviour {


    public static EditPlayerName Instance { get; private set; }


    public event EventHandler OnNameChanged;


    [SerializeField] private TextMeshProUGUI playerNameText;


    private string playerName = "User";


    private void Awake() {
        Instance = this;

        GetComponent<Button>().onClick.AddListener(() => {
            UI_InputWindow.Show_Static("Player Name", playerName, "abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ.,-", 20,
            () => {
                // Cancel
            },
            (string newName) => {
                playerName = newName;

                playerNameText.text = playerName;

                OnNameChanged?.Invoke(this, EventArgs.Empty);
            });
        });

        playerNameText.text = playerName;
    }

    private void Start() {
        OnNameChanged += EditPlayerName_OnNameChanged;
        if (OnlineManager.Instance.PlayerName == null || OnlineManager.Instance.PlayerName == String.Empty)
            EditPlayerName_OnNameChanged(gameObject, EventArgs.Empty);
        else
        {
            playerName = OnlineManager.Instance.PlayerName;
            playerNameText.text = playerName;
            EditPlayerName_OnNameChanged(gameObject, EventArgs.Empty);
        }

    }

    private void EditPlayerName_OnNameChanged(object sender, EventArgs e) {
        // LobbyManager.Instance.UpdatePlayerName(GetPlayerName());
        OnlineManager.Instance.PlayerName = playerName;
        OnlineManager.Instance.ChangeNameServerRpc(LobbyManager.Instance.GetPlayerOrCreate().Id, GetPlayerName(), NetworkManager.Singleton.LocalClientId);
    }

    public string GetPlayerName() {
        return playerName;
    }

    public void SetPlayerName(string playerName)
    {
        this.playerName = playerName;

        int index = playerName.IndexOf("_");
        if (index >= 0)
            this.playerName = playerName.Substring(0, index);
    }


}