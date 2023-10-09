using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Bullet : MonoBehaviour {
    public float speed;
    public Vector3 direction;
    public LayerMask obstacle;
    int obstacleLayer = 0;
    int playerLayer = 0;

    private GameObject parent;
    private int bulletDmg;

    private PlayerManager playerManager;


    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(WaitToDeleteServerRpc());
        obstacleLayer = LayerMask.NameToLayer("Obstacle");
        playerLayer = LayerMask.NameToLayer("Player");
        bulletDmg = 20;

        playerManager = parent.GetComponent<PlayerManager>();

    }

    [ServerRpc]
    public void SetParent(GameObject parent)
    {
        this.parent = parent;
    }


    // Update is called once per frame
    void Update()
    {

        transform.Translate(Vector3.down * speed * Time.deltaTime);
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
    private void OnTriggerEnterServerRpc(Collider other)
    {
        GameObject hitObject = other.gameObject;
        if (hitObject.layer == obstacleLayer || (hitObject.layer == playerLayer && hitObject != parent && IsEnemy(hitObject)) )
        {
            Destroy(gameObject);
            if (hitObject.layer == playerLayer)
            {
                hitObject.GetComponent<PlayerManager>().DamageTakenServerRpc(bulletDmg);
                
                //QUITAR VIDA AL JUGADOR
            }
        }
    }

    private bool IsEnemy(GameObject hitPlayer)
    {
        //  if(hitPlayer.layer == playerLayer)
        //     if(hitPlayer.GetComponent<PlayerManager>().PlayerTeam != this.playerTeam)
        return playerManager.PlayerTeam != hitPlayer.GetComponent<PlayerManager>().PlayerTeam; 
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
    private IEnumerator WaitToDeleteServerRpc()
    {
        yield return new WaitForSeconds(5);
        Destroy(gameObject);
    }
}
