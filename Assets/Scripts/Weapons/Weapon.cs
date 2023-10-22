using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Windows;

public class Weapon : MonoBehaviour
{

    [SerializeField]
    protected LineRenderer lineRenderer;

    [SerializeField]
    protected GameObject bullet;
    [SerializeField]
    protected float coolDownSeconds;

    [SerializeField]
    protected bool isReady;


    protected GameObject bulletGameObject;

    [SerializeField]
    protected Vector3 straightAim;



    private float bulletSpeed;
    private float bulletTime;


    private Vector3 bulletAimPos;
    protected virtual void Start()
    {
        isReady = true;
        Debug.Log(isReady);

        // float bulletSpeed = bullet.GetComponent<Bullet>().speed;

        bulletTime = bullet.GetComponent<Bullet>().timeToDestroy;

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ServerRpc]
    public virtual void PlayerFireServerRpc()
    {
        if (!isReady) return;
        bulletGameObject = Instantiate(bullet, transform.position, transform.rotation);
        bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
        bulletGameObject.transform.Rotate(90, 0, 0);
        bulletGameObject.GetComponent<NetworkObject>().Spawn();
        StartCoroutine(CoolDownServerRpc());
    }

    [ServerRpc]
    public virtual void PlayerFireServerRpc(Vector3 dir)
    {
        if (!isReady) return;


        Vector3 targetDirection = dir - transform.position;
        transform.forward = targetDirection;

        //Start animation and set player rotation until animation finishes


        bulletGameObject = Instantiate(bullet, transform.position, transform.rotation);
        bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
        bulletGameObject.transform.Rotate(90, 0, 0);
        bulletGameObject.GetComponent<NetworkObject>().Spawn();

        GetComponent<PlayerManager>().firing = true;
        StartCoroutine(FiringAnimation());
        StartCoroutine(CoolDownServerRpc());
    }

    [ServerRpc]
    public virtual IEnumerator CoolDownServerRpc()
    {
        isReady = false;
        yield return new WaitForSeconds(coolDownSeconds);
        isReady = true;
    }


    [ServerRpc]
    public virtual IEnumerator FiringAnimation()
    {
        yield return new WaitForSeconds(2);
        GetComponent<PlayerManager>().firing = false;
    }

    public void Awake()
    {
        
    }

    public virtual void AimWeapon()
    {
        Debug.Log("Aiming Gun!!");

        lineRenderer.positionCount = 2;


        Vector3 startVel = -gameObject.transform.forward * bullet.GetComponent<Bullet>().speed;
        bulletAimPos = startVel * bulletTime;

        Vector3 point = transform.position + bulletAimPos;

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, point);

        Debug.Log("****");
        Debug.Log(lineRenderer.positionCount);
    }

    public virtual void AimWeapon(Vector3 dir)
    {
        Vector3 targetDirection = dir - transform.position;
        transform.forward = targetDirection;
        
        
        lineRenderer.positionCount = 2;


        Vector3 startVel = targetDirection * bullet.GetComponent<Bullet>().speed;
        bulletAimPos = startVel * bulletTime;

        Vector3 point = targetDirection + bulletAimPos;

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, point);


    }

    public virtual void StopAim()
    {
        lineRenderer.enabled = false;
    }
}
