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


    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        int granadeLayer = gameObject.layer;

        for (int i = 0; i < 32; i++)
        {
            if(!Physics.GetIgnoreLayerCollision(granadeLayer, i))
            {
                granadeCollisionMask |= 1 << i;
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /*public override void PlayerFireServerRpc()
    {
    }
    */

    [ServerRpc]
    public override void PlayerFireServerRpc()
    {

        if (!isReady) return;
        bulletGameObject = Instantiate(bullet, transform.position, transform.rotation);
        bulletGameObject.GetComponent<Bullet>().SetParent(gameObject);
        //bulletGameObject.transform.Rotate(90, 0, 0);
        bulletGameObject.GetComponent<NetworkObject>().Spawn();

        Debug.Log("SPAWNED GRANADE!");

        //   StartCoroutine(CoolDownServerRpc());
        bulletGameObject.GetComponent<Granade>().ReleaseGrenade(grenadeForce, granadeInclination);
     //   ReleaseGrenade();
    }

    [ServerRpc]
    public override void StartCoolDownServerRpc()
    {
      base.StartCoolDownServerRpc();
    }

    public override void AimWeapon()
    {
        DrawProjection();
    }

    public override void StopAim()
    {
        lineRenderer.enabled = false;
    }

    private void DrawProjection()
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = Mathf.CeilToInt(LinePoints / timeBetweenPoint) + 1;
        Vector3 startPos = transform.position;
        Vector3 startVel = grenadeForce * -GetComponentInParent<Transform>().forward / bullet.GetComponent<Granade>().GetComponent<Rigidbody>().mass;
        startVel.y += granadeInclination;
        int i = 0;

        lineRenderer.SetPosition(i, startPos);
        for(float time = 0; time < LinePoints; time += timeBetweenPoint)
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
