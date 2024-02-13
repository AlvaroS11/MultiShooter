using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Windows;
using System;

public class Melee : Weapon
{
    [SerializeField]
    private int nBullets = 3;

    [SerializeField]
    private float timeBetweenBullets = 1f;

    [SerializeField]
    private float meleeRangeMultiplier = 2f;


    protected override void Start()
    {
        base.Start();
    }
    

    protected override void Update()
    {
        base.Update();
    }



    public override void AimWeapon(Vector3 dir)
    {
        lineRenderer.enabled = true;

        Vector3 targetDirection = dir - transform.position;

        
        lineRenderer.positionCount = 2;

        Vector3 point = transform.position + targetDirection.normalized*meleeRangeMultiplier;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, point);
    }


    public override Vector3 AimWeaponMobile(Vector3 dir)
    {
        dir = dir.normalized;

        //Debug.Log(dir);

        Vector3 targetDirection = dir + transform.position;
        lineRenderer.positionCount = 2;

        Vector3 startVel = dir * bullet.GetComponent<Bullet>().speed;
        Vector3 bulletAimPos = startVel * meleeRangeMultiplier/2;

        Vector3 point = targetDirection + bulletAimPos;


        //Vector3 point = transform.position + targetDirection.normalized * meleeRangeMultiplier;

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, point);

        return point;   
    }

    public override void StopAim()
    {
        base.StopAim();
    }

    private void DrawProjection()
    {
        lineRenderer.enabled = true;
    }

    [ServerRpc]
    public override void PlayerFireServerRpc(Vector3 dir, ulong clientId)
    {
        if (!isReady) return;

        // Vector3 targetDirection = dir - transform.position;

        //transform.forward = targetDirection;
        previousTimeStamp = DateTime.Now;
        ShootIsLocked = true;

        GetComponent<PlayerManager>().firing.Value = true;
        FiringAnimClientRpc();


        StartCoroutine(Fire());


        cooldownCoroutine = StartCoroutine(CoolDown());

        //StartCoolDownServerRpc();

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        StartReloadAnimationClientRpc(clientRpcParams);
        ShootSoundClientRpc();
        ShootIsLocked = false;

    }



    [ServerRpc(RequireOwnership = false)]
    public override void StartCoolDownServerRpc()
    {
        base.StartCoolDownServerRpc();
    }


    [ClientRpc]
    private void FiringAnimClientRpc()
    {
        StartCoroutine(FiringAnimation());
    }

    [ServerRpc]
    public override void PlayerFireServerMobileServerRpc(Vector3 dir, ulong clientId)
    {
        Vector3 targetDirection = dir - transform.position;
        transform.forward = targetDirection;
        PlayerFireServerRpc(dir, clientId);
    }

    private IEnumerator Fire()
    {
        bulletGameObject = Instantiate(bullet, transform.position, transform.rotation);
        bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
        bulletGameObject.GetComponent<Bullet>().playerManager = GetComponent<PlayerManager>();
        yield return new WaitForSeconds(1);
        
        Destroy(bulletGameObject);
    }

}
