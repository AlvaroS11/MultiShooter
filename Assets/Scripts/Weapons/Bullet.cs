using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using System;

public class Bullet : MonoBehaviour {
    public float speed;
    public Vector3 direction;
    public LayerMask obstacle;
    protected int obstacleLayer = 0;
    protected int playerLayer = 0;

    protected GameObject parent;

    [SerializeField]
    protected int bulletDmg = 20;

    protected PlayerManager playerManager;


    [SerializeField]
    public int timeToDestroy = 5;
    //public NetworkVariable<int> bulletDmg;


    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(WaitToDeleteServerRpc());
        obstacleLayer = LayerMask.NameToLayer("Obstacle");
        playerLayer = LayerMask.NameToLayer("Player");

        playerManager = parent.GetComponent<PlayerManager>();


        //Calculate range with speed and timeToDestroy

    }

    [ServerRpc]
    public void SetParent(GameObject parent)
    {
        this.parent = parent;
    }


    // Update is called once per frame
    void Update()
    {

        transform.Translate(Vector3.up * speed * Time.deltaTime);
     //   if(direction == Vector3.zero)
        //    Debug.Log(direction);
      //  else
      //      direction = transform.up.normalized;
        //if(IsOwner)
        // MoveServerRpc();
        // transform.Translate((direction * speed * Time.deltaTime));   
        //  Vector3.MoveTowards(transform.position, transform.forward, speed*Time.deltaTime);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        OnTriggerEnterServerRpc(other);
    }

    [ServerRpc]
    public virtual void OnTriggerEnterServerRpc(Collider other)
    {
        GameObject hitObject = other.gameObject;
        if (hitObject.layer == obstacleLayer || (hitObject.layer == playerLayer && hitObject != parent && IsEnemy(hitObject)) )
        {
            Destroy(gameObject);
            if (hitObject.layer == playerLayer)
            {
                PlayerManager hitPlayerManager = hitObject.GetComponent<PlayerManager>();

                hitPlayerManager.DamageTakenServerRpc(bulletDmg, playerManager.PlayerInfoIndex, hitPlayerManager.PlayerInfoIndex);
                
                //QUITAR VIDA AL JUGADOR
            }
        }
    }

    protected virtual bool IsEnemy(GameObject hitPlayer)
    {
        //  if(hitPlayer.layer == playerLayer)
        //     if(hitPlayer.GetComponent<PlayerManager>().PlayerTeam != this.playerTeam)
        //return playerManager.PlayerTeam != hitPlayer.GetComponent<PlayerManager>().PlayerTeam; 
        try
        {
            return playerManager.PlayerTeam != hitPlayer.GetComponent<PlayerManager>().PlayerTeam;
        }
        catch(Exception e)
        {
            Debug.Log("PlayerManager not found, error: " + e);
            return false;
        }



        Debug.Log(playerManager.PlayerTeam != hitPlayer.GetComponent<PlayerManager>().PlayerTeam);
        return true;
    }


    [ServerRpc]
    private void MoveServerRpc()
    {
        transform.Translate(speed * Time.deltaTime * direction);

        if (transform.position == direction)
            Debug.Log("--------");

    }

    [ServerRpc]
    public void BulletDirectionServerRpc(Vector3 newDirection)
    {

        direction = newDirection.normalized;
    }


    [ServerRpc]
    public virtual IEnumerator WaitToDeleteServerRpc()
    {
        yield return new WaitForSeconds(timeToDestroy);
        Destroy(gameObject);
    }
}
