using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Assets : MonoBehaviour
{
    // Start is called before the first frame update

    public static Assets Instance;


    public GameObject respawnMsg;

    public TextMeshProUGUI respawnText;
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
