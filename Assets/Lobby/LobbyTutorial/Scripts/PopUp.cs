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
        messageText.text = message;
        gameObject.SetActive(true);
        button1.gameObject.active = showButtons; 
        button2.gameObject.SetActive(showButtons);
        TextColor(type);
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
        gameObject.SetActive(false);
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
