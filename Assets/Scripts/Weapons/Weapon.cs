using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System;


public class Weapon : NetworkBehaviour
{

    [SerializeField]
    protected LineRenderer lineRenderer;

    [SerializeField]
    protected GameObject bullet;
    [SerializeField]
    protected float coolDownSeconds;

    [SerializeField]
    protected float currentReload;

    [SerializeField]
    protected bool isReady;


    protected GameObject bulletGameObject;


    [SerializeField]
    protected bool straightAim = true;


    private float bulletSpeed;
    private float bulletTime;


    private Vector3 bulletAimPos;


    public Image reloadBar;  //fillamount uiplayer


    public bool reloading = false;

    public AudioSource shotSound;


    private PlayerManager playerManager;
    protected virtual void Start()
    {
        playerManager = GetComponent<PlayerManager>();
        isReady = true;

        // float bulletSpeed = bullet.GetComponent<Bullet>().speed;

        bulletTime = bullet.GetComponent<Bullet>().timeToDestroy;


        if (IsOwner)
        {
            reloadBar = Assets.Instance.reloadBar;
        }
    }

    protected virtual void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            reloadBar = Assets.Instance.reloadBar;
        }
    }

    public DateTime previousTimeStamp = DateTime.Now;
    protected Coroutine cooldownCoroutine = null;
    // Update is called once per frame
    protected virtual void Update()
    {
        if (IsOwner)
        {
            if (reloading)
            {
                currentReload -= Time.deltaTime;
                if(currentReload >= 0)
                    reloadBar.fillAmount = 1 - (currentReload / coolDownSeconds);
                else
                {
                    reloading = false;
                 //   playerManager.localFiring = false;
                }
                    
            }
        }

        if (IsServer)
        {
            if (cooldownCoroutine != null)
                return;
            else if ((DateTime.Now - previousTimeStamp).Seconds > coolDownSeconds && playerManager.firing.Value == true)
            {
                playerManager.firing.Value = false;
                isReady = true;
                previousTimeStamp = DateTime.Now;
            };
        }
       
    }

    [ServerRpc]
    public virtual void PlayerFireServerRpc(Vector3 dir, ulong clientId)
    {
        if (!isReady) return;
        StartCoroutine(FiringAnimation());


        Vector3 targetDirection = dir - transform.position;
      //  transform.forward = targetDirection;

        Transform bulletPos = transform;
        bulletPos.forward = targetDirection;


        //Start animation and set player rotation until animation finishes

        bulletGameObject = Instantiate(bullet, transform.position, transform.rotation);
        Bullet bulletInstance = bulletGameObject.GetComponent<Bullet>();
        bulletInstance.SetParent(gameObject);
        bulletInstance.playerManager = GetComponent<PlayerManager>();


        bulletGameObject.transform.Rotate(90, 0, 0);
        bulletGameObject.GetComponent<NetworkObject>().Spawn();


        GetComponent<PlayerManager>().firing.Value = true;
        
        StartCoolDownServerRpc();

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        StartReloadAnimationClientRpc(clientRpcParams);

        //shotSound.Play();
        ShootSoundClientRpc();

    }

    [ClientRpc]
    public void ShootSoundClientRpc()
    {
        Debug.Log("sound shoot");
        shotSound.Play();
    }



    [ServerRpc]
    public virtual void PlayerFireServerMobileServerRpc(Vector3 dir, ulong clientId)
    {
        Vector3 targetDirection = dir - transform.position;
        transform.forward = targetDirection;

        if (!isReady) return;


        transform.forward = targetDirection;


        StartCoroutine(FiringAnimation());

        bulletGameObject = Instantiate(bullet, transform.position, transform.rotation);
        bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
        bulletGameObject.transform.Rotate(90, 0, 0);
        bulletGameObject.GetComponent<NetworkObject>().Spawn();

        //GetComponent<PlayerManager>().firing.Value = true;

        cooldownCoroutine = StartCoroutine(CoolDown());

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        StartReloadAnimationClientRpc(clientRpcParams);
        //shotSound.Play();
        ShootSoundClientRpc();

    }


    [ClientRpc]
    protected void StartReloadAnimationClientRpc(ClientRpcParams clientRpcParams = default)
    {
        currentReload = coolDownSeconds;
        reloading = true;
        reloadBar.fillAmount = 0;

    }



    [ServerRpc]
    public virtual void StartCoolDownServerRpc()
    {
        StartCoroutine(CoolDown());
    }


    public virtual IEnumerator CoolDown()
    {

        isReady = false;
        PlayerManager pManager = GetComponent<PlayerManager>();
        pManager.firing.Value = true;
        yield return new WaitForSeconds(coolDownSeconds);
        isReady = true;
        pManager.firing.Value = false;

        GetComponent<PlayerManager>().bodyAnimator.SetBool("firing", false);
        cooldownCoroutine = null;
        previousTimeStamp = DateTime.Now;
    }

    [ServerRpc]
    private void StartFiringAnimationServerRpc()
    {
        StartCoroutine(FiringAnimation());
    }

    public virtual IEnumerator FiringAnimation()
    {
        PlayerManager pManager = GetComponent<PlayerManager>();
        pManager.bodyAnimator.SetBool("firing", true);
        yield return new WaitForSeconds(1);
       // pManager.firing.Value = false;
        pManager.bodyAnimator.SetBool("firing", false);
     //   pManager.localFiring = false;
    }



    public void Awake()
    {
        
    }

    public virtual void AimWeapon()
    {
        lineRenderer.positionCount = 2;


        Vector3 startVel = -gameObject.transform.forward * bullet.GetComponent<Bullet>().speed;
     //   bulletAimPos = startVel * bulletTime;

        Vector3 point = transform.position + bulletAimPos;

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, point);
    }

    public virtual void AimWeapon(Vector3 dir)
    {
        Vector3 targetDirection = dir - transform.position;
       // transform.forward = targetDirection;
        
        
        lineRenderer.positionCount = 2;


        Vector3 startVel = targetDirection * bullet.GetComponent<Bullet>().speed;
        bulletAimPos = startVel * bulletTime;

        Vector3 point = targetDirection + bulletAimPos;

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, point);
    }

    public virtual Vector3 AimWeaponMobile(Vector3 dir)
    {
        if (straightAim)
            dir = dir.normalized;

        Vector3 targetDirection = dir + transform.position;


        lineRenderer.positionCount = 2;


        Vector3 startVel = dir * bullet.GetComponent<Bullet>().speed;
        bulletAimPos = startVel * bulletTime;

        Vector3 point = targetDirection + bulletAimPos;
   
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, point);

        return point;

    }

    public virtual void StopAim()
    {
        lineRenderer.enabled = false;
    }
}
