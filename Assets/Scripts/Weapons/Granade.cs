using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class Granade : Bullet
{
    // Start is called before the first frame update
    void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(WaitToDeleteServerRpc());
        obstacleLayer = LayerMask.NameToLayer("Obstacle");
        playerLayer = LayerMask.NameToLayer("Player");
        bulletDmg = 20;

        playerManager = parent.GetComponent<PlayerManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    


}
