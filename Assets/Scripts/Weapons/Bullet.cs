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

    public AudioSource audioSource;


    private void Start()
    {
        SetUp();
    }
   
    private void Awake()
    {
        SetUp();
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
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        OnTriggerEnterServerRpc(other);
    }

    [ServerRpc]
    public virtual void OnTriggerEnterServerRpc(Collider other, bool destroyBullet = true)
    {
        GameObject hitObject = other.gameObject;
        if (hitObject.layer == obstacleLayer || (hitObject.layer == playerLayer && hitObject != parent && IsEnemy(hitObject)) )
        {
            if(destroyBullet)
                Destroy(gameObject);
            if (hitObject.layer == playerLayer)
            {
                PlayerManager hitPlayerManager = hitObject.GetComponent<PlayerManager>();

                if(!hitPlayerManager.isInmune.Value)
                    hitPlayerManager.DamageTakenServerRpc(bulletDmg, playerManager.PlayerInfoIndex, hitPlayerManager.PlayerInfoIndex);
            }
        }
    }

    protected virtual bool IsEnemy(GameObject hitPlayer)
    {
        try
        {
            if(playerManager == null)
                playerManager = parent.GetComponent<PlayerManager>();

            if (LobbyManager.Instance.m_gameMode == LobbyManager.GameMode.Free_for_all)
            {
                return true;
            }
          return playerManager.PlayerTeam.Value != hitPlayer.GetComponent<PlayerManager>().PlayerTeam.Value;
        }
        catch(Exception e)
        {
            Debug.LogError("PlayerManager not found, error: " + e);
            Debug.LogError(parent.GetComponent<PlayerManager>().name);
            return false;
        }
    }


    [ServerRpc]
    public virtual IEnumerator WaitToDeleteServerRpc()
    {
        yield return new WaitForSeconds(timeToDestroy);
        Destroy(gameObject);
    }
}
