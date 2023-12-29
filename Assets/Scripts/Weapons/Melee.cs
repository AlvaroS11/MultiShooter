using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Drawing;

public class Melee : Weapon
{
    [SerializeField]
    private int nBullets = 3;

    [SerializeField]
    private float timeBetweenBullets = 1f;
    // Start is called before the first frame update


    [SerializeField]
    private BoxCollider boxHit;
    protected override void Start()
    {
        base.Start();

        boxHit.enabled = false;
    }

    

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    /*public override void AimWeapon()
    {
        //base.AimWeapon();
        lineRenderer.enabled = true;


    }
    */


    public override void AimWeapon(Vector3 dir)
    {
        //base.AimWeapon();
        lineRenderer.enabled = true;

        Vector3 targetDirection = dir - transform.position;

        
        lineRenderer.positionCount = 2;

       //da Vector3 point = targetDirection.normalized * 2;
        Vector3 point = transform.position + targetDirection.normalized*2;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, point);
    }

    public override void StopAim()
    {
        base.StopAim();
    }

    private void DrawProjection()
    {
        lineRenderer.enabled = true;
    }

    /*[ServerRpc]
    public override void PlayerFireServerRpc()
    {

        if (!isReady) return;
        StartCoolDownServerRpc();

        //3 times
    }
    */
    [ServerRpc]
    public override void PlayerFireServerRpc(Vector3 dir, ulong clientId)
    {
        if (!isReady) return;


        Debug.Log("FIRING");
        Vector3 targetDirection = dir - transform.position;
        transform.forward = targetDirection;

        //Start animation and set player rotation until animation finishes

        GetComponent<PlayerManager>().firing = true;
        StartCoroutine(FiringAnimation());


        StartCoroutine(Fire());

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


   /* public void Fire()
    {
        boxHit.enabled = true;

    }*/

    private IEnumerator Fire()
    {
        bulletGameObject = Instantiate(bullet, transform.position, transform.rotation);
        bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
        bulletGameObject.GetComponent<Bullet>().playerManager = GetComponent<PlayerManager>();
        yield return new WaitForSeconds(1);

        Destroy(bulletGameObject);
    }

}
