using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Assets : MonoBehaviour
{
    // Start is called before the first frame update

    public static Assets Instance;


    public GameObject respawnMsg;

    public TextMeshProUGUI respawnText;

    public Image reloadBar;

    public Joystick joystick;

    public Joystick joystickShoot;

   /* void Start()
    {
        Instance = this;
        Debug.Log("OOOOOOOOOO");
    }
   */

    private void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
