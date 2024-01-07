using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSingleton : MonoBehaviour
{

    public static NetworkSingleton Instance { get; private set; }

    void Start()
    {
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
}
