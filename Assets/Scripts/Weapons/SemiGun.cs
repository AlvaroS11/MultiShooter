using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SemiGun : Weapon
{
    [SerializeField]
    private int nBullets = 3;

    [SerializeField]
    private float timeBetweenBullets = 1f;
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }

    

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    public override void AimWeapon()
    {
        base.AimWeapon();
    }

    public override void StopAim()
    {
        base.StopAim();
    }

    private void DrawProjection()
    {
        lineRenderer.enabled = true;
    }

  /*  [ServerRpc]
    public override void PlayerFireServerRpc()
    {

        if (!isReady) return;

        StartWaitBulletsServerRpc();
        StartCoolDownServerRpc();

        //3 times
    }*/

    [ServerRpc]
    public override void PlayerFireServerRpc(Vector3 dir, ulong clientId)
    {
        Debug.Log("ES ESTE!");
        if (!isReady) return;


        Vector3 targetDirection = dir - transform.position;
        transform.forward = targetDirection;

        //Start animation and set player rotation until animation finishes

        GetComponent<PlayerManager>().firing = true;
        StartCoroutine(FiringAnimation());

        StartWaitBulletsServerRpc();

        /*bulletGameObject = Instantiate(bullet, transform.position, transform.rotation);
        bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
        bulletGameObject.transform.Rotate(90, 0, 0);
        bulletGameObject.GetComponent<NetworkObject>().Spawn();
        */






        // StartCoroutine(CoolDownServerRpc());
        StartCoolDownServerRpc();

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        StartReloadAnimationClientRpc(clientRpcParams);
    }




    [ServerRpc]
    private void StartWaitBulletsServerRpc()
    {
        StartCoroutine(WaitBullets());
    }

    public virtual IEnumerator WaitBullets()
    {
        for (int i = 0; i < nBullets; i++)
        {
            Debug.Log("BULLET " + i);
            bulletGameObject = Instantiate(bullet, transform.position, transform.rotation);
            bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
            bulletGameObject.transform.rotation = Quaternion.Euler(90, transform.rotation.eulerAngles.y, 0);

            bulletGameObject.GetComponent<NetworkObject>().Spawn();
            yield return new WaitForSeconds(timeBetweenBullets);
        }
    }

}
