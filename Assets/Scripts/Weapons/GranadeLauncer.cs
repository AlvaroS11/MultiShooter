using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GranadeLauncer : Weapon
{

    public Transform releasePosition;

    [Header("Display Controls")]
    [SerializeField]
    [Range(10, 100)]
    private int LinePoints = 26;
    [Range(0.01f, 0.25f)]
    public float timeBetweenPoint = 0.1f;

    public float granadeInclination = 20f;


    public LayerMask granadeCollisionMask;

  //  public Rigidbody Grenade;
    public float grenadeForce = 20f;



    protected override void Start()
    {
        base.Start();
        int granadeLayer = gameObject.layer;

        for (int i = 0; i < 19; i++)
        {
            if(!Physics.GetIgnoreLayerCollision(granadeLayer, i))
            {
                granadeCollisionMask |= 1 << i;
            }
        }

    }

    protected override void Update()
    {
        base.Update();
    }



  /*  [ServerRpc]
    public override void PlayerFireServerRpc()
    {

        if (!isReady) return;
        bulletGameObject = Instantiate(bullet, transform.position, transform.rotation);
        bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
        //bulletGameObject.transform.Rotate(90, 0, 0);
        bulletGameObject.GetComponent<NetworkObject>().Spawn();


        bulletGameObject.GetComponent<Granade>().ReleaseGrenade(grenadeForce, granadeInclination);
        shotSound.Play();
    }*/

    [ServerRpc]
    public override void PlayerFireServerRpc(Vector3 dir, ulong clientId)
    {
        if (!isReady) return;
        Vector3 bulletPos = new Vector3(transform.position.x, transform.position.y + 0.2f, transform.position.z);
        bulletGameObject = Instantiate(bullet, bulletPos, transform.rotation);
        bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
        bulletGameObject.GetComponent<NetworkObject>().Spawn();

        Vector3 targetDirection = dir - transform.position;

        transform.forward = targetDirection;


        bulletGameObject.GetComponent<Granade>().ReleaseGrenade(grenadeForce, granadeInclination, dir);
        //bulletGameObject.GetComponent<Granade>().ReleaseGrenade(grenadeForce, granadeInclination);

        base.StartCoolDownServerRpc();

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        StartReloadAnimationClientRpc(clientRpcParams);
        shotSound.Play();
    }

    [ServerRpc]
    public override void StartCoolDownServerRpc()
    {
      base.StartCoolDownServerRpc();
    }

    public override void AimWeapon()
    {
      //  DrawProjection();
    }

    public override void AimWeapon(Vector3 dir)
    {
        DrawProjection(dir);

        //DrawProjectionMobile(dir);

    }

    public override Vector3 AimWeaponMobile(Vector3 dir)
    {
        DrawProjectionMobile(dir);
        return dir;


    }

    public override void StopAim()
    {
        lineRenderer.enabled = false;
    }

    [ServerRpc]
    public override void PlayerFireServerMobileServerRpc(Vector3 dir, ulong clientId)
    {
        if (!isReady) return;

        FireMobile(dir);

        base.StartCoolDownServerRpc();

        Vector3 targetDirection = dir - transform.position;

        transform.forward = targetDirection;

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        StartReloadAnimationClientRpc(clientRpcParams);
        shotSound.Play();
    }

    private void FireMobile(Vector3 dir)
    {
        Vector3 bulletPos = new Vector3(transform.position.x, transform.position.y + 0.2f, transform.position.z);

        bulletGameObject = Instantiate(bullet, bulletPos, transform.rotation);
        bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
        bulletGameObject.GetComponent<NetworkObject>().Spawn();
        bulletGameObject.GetComponent<Granade>().ReleaseGrenadeMobile(grenadeForce, granadeInclination, dir);
    }




    private void DrawProjectionMobile(Vector3 dir)
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = Mathf.CeilToInt(LinePoints / timeBetweenPoint) + 1;
        Vector3 startPos = transform.position;
        Vector3 startVel = grenadeForce * dir / bullet.GetComponent<Granade>().GetComponent<Rigidbody>().mass;
        startVel.y += granadeInclination;
        int i = 0;

        lineRenderer.SetPosition(i, startPos);
        for (float time = 0; time < LinePoints; time += timeBetweenPoint)
        {
            i++;
            Vector3 point = startPos + time * startVel;
            point.y = startPos.y + startVel.y * time + (Physics.gravity.y / 2f * time * time); // y = vi * t +1/2 * a * t2

            lineRenderer.SetPosition(i, point);

            Vector3 lastPos = lineRenderer.GetPosition(i - 1);
            if (Physics.Raycast(lastPos, (point - lastPos).normalized, out RaycastHit hit, (point - lastPos).magnitude, granadeCollisionMask))
            {
                lineRenderer.SetPosition(i, hit.point);
                lineRenderer.positionCount = i + 1;
                return;
            }
        }

    }

    private void DrawProjection(Vector3 finalPos)
    {
             lineRenderer.enabled = true;
             lineRenderer.positionCount = Mathf.CeilToInt(LinePoints / timeBetweenPoint) + 1;
             Vector3 startPos = transform.position;


             Vector3 startVel = finalPos - transform.position;

            Vector3 newVel = Vector3.ClampMagnitude(startVel/2, grenadeForce);

             newVel.y += granadeInclination;


            int i = 0;
        

             lineRenderer.SetPosition(i, startPos);
             for(float time = 0; time < LinePoints; time += timeBetweenPoint)
             {
                 i++;
                 Vector3 point = startPos + time * newVel;
                 point.y = startPos.y + newVel.y * time + (Physics.gravity.y / 2f * time * time); // y = vi * t +1/2 * a * t2

                 lineRenderer.SetPosition(i, point);

                 Vector3 lastPos = lineRenderer.GetPosition(i - 1);
                 if (Physics.Raycast(lastPos, (point - lastPos).normalized, out RaycastHit hit, (point - lastPos).magnitude, granadeCollisionMask)) 
                 {
                     lineRenderer.SetPosition(i, hit.point);
                     lineRenderer.positionCount = i + 1;
                     return;
                 }
             }
             //lineRenderer.SetPosition(i, finalPos);

    }

   


    private void ReleaseGrenade()
    {
        Rigidbody Grenade = bulletGameObject.GetComponent<Rigidbody>();
        Grenade.velocity = Vector3.zero;
        Grenade.angularVelocity = Vector3.zero;
        Grenade.isKinematic = false;
        Grenade.freezeRotation = false;
        Grenade.transform.SetParent(null, true);

        Vector3 dir = grenadeForce * -GetComponentInParent<Transform>().forward;
        dir.y = granadeInclination;
        Grenade.AddForce(dir, ForceMode.Impulse);

      //  StartCoroutine(ExplodeGrenade());
    }

   /* private IEnumerator ExplodeGrenade()
    {
        yield return new WaitForSeconds(ExplosionDelay);

        Instantiate(ExplosionParticleSystem, Grenade.transform.position, Quaternion.identity);

        Grenade.GetComponent<Cinemachine.CinemachineImpulseSource>().GenerateImpulse(new Vector3(Random.Range(-1, 1), Random.Range(0.5f, 1), Random.Range(-1, 1)));

        Grenade.freezeRotation = true;
        Grenade.isKinematic = true;
        Grenade.transform.SetParent(InitialParent, false);
        Grenade.rotation = InitialRotation;
        Grenade.transform.localPosition = InitialLocalPosition;
        IsGrenadeThrowAvailable = true;
    }*/

    //public void OnPointer


}
