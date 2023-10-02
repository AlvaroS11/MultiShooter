using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Weapon : MonoBehaviour
{

    public GameObject bullet;
    public float coolDownSeconds;

    [SerializeField]
    private bool isReady;
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
    public void PlayerFireServerRpc()
    {
        if (!isReady) return;
        GameObject bulletGameObject = Instantiate(bullet, transform.position, transform.rotation);
        bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
        bulletGameObject.transform.Rotate(90, 0, 0);
        bulletGameObject.GetComponent<NetworkObject>().Spawn();
        StartCoroutine(CoolDownServerRpc());
    }

    [ServerRpc]
    private IEnumerator CoolDownServerRpc()
    {
        isReady = false;
        yield return new WaitForSeconds(coolDownSeconds);
        isReady = true;
    } 
}
