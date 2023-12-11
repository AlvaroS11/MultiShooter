using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System;

public class Bullet : MonoBehaviour {
    public float speed;
    public Vector3 direction;
    public LayerMask obstacle;
    protected int obstacleLayer = 0;
    [SerializeField]
    protected int playerLayer = 0;

    protected GameObject parent;

    [SerializeField]
    protected int bulletDmg = 20;

    public PlayerManager playerManager;


    [SerializeField]
    public int timeToDestroy = 5;
    //public NetworkVariable<int> bulletDmg;

    [SerializeField]
    private bool shouldFollow = false;

    private void Start()
    {
       /* if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(WaitToDeleteServerRpc());
        obstacleLayer = LayerMask.NameToLayer("Obstacle");
        playerLayer = LayerMask.NameToLayer("Player");


        if(playerManager == null)
        {
            playerManager = parent.GetComponent<PlayerManager>();
            Debug.Log("playermanager was null");
        }

        Debug.Log("parent is " + playerManager.gameObject.name);
        //Calculate range with speed and timeToDestroy*/

        SetUp();
    }
   
    private void Awake()
    {
        SetUp();


      /*  if (playerManager == null)
        {
            playerManager = parent.GetComponent<PlayerManager>();
            Debug.Log("playermanager was null");
        }*/

        //Debug.Log("parent is " + playerManager.gameObject.name);
    }


    private void SetUp()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(WaitToDeleteServerRpc());
        obstacleLayer = LayerMask.NameToLayer("Obstacle");
        playerLayer = LayerMask.NameToLayer("Player");
    }
    [ServerRpc]
    public void SetParent(GameObject parent)
    {
        this.parent = parent;
    }


    // Update is called once per frame
    void Update()
    {
        if(!shouldFollow)
            transform.Translate(Vector3.up * speed * Time.deltaTime);
        else
            transform.position = parent.transform.position;
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
        Debug.Log("COLLISION ");
    }

    [ServerRpc]
    public virtual void OnTriggerEnterServerRpc(Collider other)
    {
        GameObject hitObject = other.gameObject;
        Debug.Log("****");
        Debug.Log(other.gameObject.name);

        Debug.Log(hitObject.layer);
        Debug.Log(IsEnemy(hitObject));

        Debug.Log(hitObject.layer == playerLayer);
        Debug.Log("*** " + hitObject.layer);
        Debug.Log(playerLayer);

        Debug.Log(hitObject != parent);
        Debug.Log(IsEnemy(hitObject));
        if (hitObject.layer == obstacleLayer || (hitObject.layer == playerLayer && hitObject != parent && IsEnemy(hitObject)) )
        {
            Debug.Log("entra1");
            Destroy(gameObject);
            if (hitObject.layer == playerLayer)
            {
                Debug.Log("entra2");
                PlayerManager hitPlayerManager = hitObject.GetComponent<PlayerManager>();

                if(!hitPlayerManager.isInmune.Value)
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
            Debug.Log(playerManager.PlayerTeam.Value);
            Debug.Log(hitPlayer.GetComponent<PlayerManager>().PlayerTeam.Value);
            return playerManager.PlayerTeam.Value != hitPlayer.GetComponent<PlayerManager>().PlayerTeam.Value;
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
