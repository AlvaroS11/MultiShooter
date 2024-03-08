using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class AuthenticateUI : MonoBehaviour {


    [SerializeField] private Button authenticateButton;

    public static AuthenticateUI Instance;

    private void Awake() {
        authenticateButton.onClick.AddListener(async () => {
            if(await LobbyManager.Instance.Authenticate(EditPlayerName.Instance.GetPlayerName()))
                Hide();
        });
        Instance = this;
        Debug.Log("auth");
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

}