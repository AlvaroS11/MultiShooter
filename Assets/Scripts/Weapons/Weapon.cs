using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField]
    protected GameObject bullet;
    [SerializeField]
    protected float coolDownSeconds;

    [SerializeField]
    protected bool isReady;


    public GameObject bulletGameObject;
    protected virtual void Start()
    {
        isReady = true;
        Debug.Log(isReady);
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
    public virtual IEnumerator CoolDownServerRpc()
    {
        isReady = false;
        yield return new WaitForSeconds(coolDownSeconds);
        isReady = true;
    }

    public void Awake()
    {
        
    }

    public virtual void AimWeapon()
    {

    }

    public virtual void StopAim()
    {

    }
}
