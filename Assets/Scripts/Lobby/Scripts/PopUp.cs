using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class PopUp : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public Button button1;
    public Button button2;

    public delegate void ButtonClickAction();
    public static event ButtonClickAction OnButton1Click;
    public static event ButtonClickAction OnButton2Click;

    public static PopUp Instance;

    public Transform pos1;
    public Transform pos2;
    public Transform pos3;

    public enum PopUpType
    {
        Info,
        Warning,
        Error
    };



    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(this);

        button1.onClick.AddListener(() =>
        {
            Button1Clicked();
        });
        button2.onClick.AddListener(() =>
        {
            Button2Clicked();
        });

        gameObject.SetActive(false);
    }



        public void ShowPopUp(string message, bool showButtons, PopUpType type)
    {
        Debug.Log("show popup");
        messageText.text = message;
        gameObject.SetActive(true);
        if (type == PopUpType.Error && showButtons == false)
        {
            button1.gameObject.active = false;

            button2.gameObject.active = true;
            button2.transform.position = new Vector3(pos1.position.x, button1.transform.position.y, button1.transform.position.z);

            OnButton2Click += () => DefaultErrorMsg();
            OnButton2Click -= () => DefaultErrorMsg();

        }
        else
        {
            button1.gameObject.active = showButtons;
            button2.gameObject.SetActive(showButtons);
        }
        TextColor(type);
        Debug.Log(message);
    }

    public void DefaultErrorMsg()
    {
        HidePopUp();
        PopUp.Instance.button2.transform.position = new Vector3(PopUp.Instance.pos3.position.x, PopUp.Instance.button1.transform.position.y, PopUp.Instance.button1.transform.position.z);
    }

    private void TextColor(PopUpType type)
    {
        switch (type)
        {
            case PopUpType.Info:
                messageText.color = Color.black; 
                break;
            case PopUpType.Error: 
                messageText.color = Color.red;
                break;
            case PopUpType.Warning:
                messageText.color = Color.yellow;
                break;
        }
    }

    public void HidePopUp()
    {
        PopUp.Instance.gameObject.SetActive(false);
    }

    public void Button1Clicked()
    {
        if (OnButton1Click != null)
            OnButton1Click();

        HidePopUp();
    }

    public void Button2Clicked()
    {
        if (OnButton2Click != null)
            OnButton2Click();

        HidePopUp();
    }
}
