using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class InputWindow : MonoBehaviour
{
    private static InputWindow instance;

    public ButtonController okBtn;
    public ButtonController cancelBtn;
    public TextMeshProUGUI titleText;
    public TMP_InputField inputField;

    private void Awake()
    {
        instance = this;

      //  okBtn = transform.Find("okBtn").GetComponent<ButtonController>();
      //  cancelBtn = transform.Find("cancelBtn").GetComponent<ButtonController>();
      //  titleText = transform.Find("titleText").GetComponent<TextMeshProUGUI>();
     //   inputField = transform.Find("inputField").GetComponent<TMP_InputField>();

        Hide();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            okBtn.ClickFunc();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            cancelBtn.ClickFunc();
        }
    }

    private void Show(string titleString, string inputString, string validCharacters, int characterLimit, Action onCancel, Action<string> onOk)
    {
        Debug.Log("Show");
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        titleText.text = titleString;

        Debug.Log("texttt");

        inputField.characterLimit = characterLimit;
        inputField.onValidateInput = (string text, int charIndex, char addedChar) =>
        {
            return ValidateChar(validCharacters, addedChar);
        };

        Debug.Log("Validadet imput");

        inputField.text = inputString;
        inputField.Select();
        Debug.Log("previous OK");

        okBtn.ClickFunc = () =>
        {
            Debug.Log("clickFunc");
            Hide();
            onOk(inputField.text);
        };

        cancelBtn.ClickFunc = () =>
        {
            Debug.Log("cancelClickFunc");
            Hide();
            onCancel();
        };
        Debug.Log("ddd");
    }

    private void Hide()
    {
        Debug.Log("HIDE");
        gameObject.SetActive(false);
    }

    private char ValidateChar(string validCharacters, char addedChar)
    {
        if (validCharacters.IndexOf(addedChar) != -1)
        {
            // Valid
            return addedChar;
        }
        else
        {
            // Invalid
            return '\0';
        }
    }

    public static void Show_Static(string titleString, string inputString, string validCharacters, int characterLimit, Action onCancel, Action<string> onOk)
    {
        Debug.Log("SHOW STATIC");
        Debug.Log(titleString);
        Debug.Log(inputString);

        Debug.Log(validCharacters);
        Debug.Log(characterLimit);
        Debug.Log(onCancel);
        Debug.Log(onOk);




        instance.Show(titleString, inputString, validCharacters, characterLimit, onCancel, onOk);
        Debug.Log("terminabn");
    }

    public static void Show_Static(string titleString, int defaultInt, Action onCancel, Action<int> onOk)
    {
        instance.Show(titleString, defaultInt.ToString(), "0123456789-", 20, onCancel,
            (string inputText) =>
            {
                // Try to Parse input string
                if (int.TryParse(inputText, out int _i))
                {
                    onOk(_i);
                }
                else
                {
                    onOk(defaultInt);
                }
            }
        );
    }
}
