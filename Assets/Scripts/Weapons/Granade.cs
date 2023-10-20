using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class Granade : Bullet
{
    [SerializeField]
    private int timeToExplode = 5;

    [SerializeField]
    private int radius = 5;


    [SerializeField]
    private GameObject ExplosionParticleSystem;

    [SerializeField]
    private int effectTime;
    void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(WaitToDeleteServerRpc());
        obstacleLayer = LayerMask.NameToLayer("Obstacle");
        playerLayer = LayerMask.NameToLayer("Player");
        bulletDmg = 20;

        playerManager = parent.GetComponent<PlayerManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ReleaseGrenade(float grenadeForce, float granadeInclination)
    {
        Rigidbody Grenade = GetComponent<Rigidbody>();
        Grenade.velocity = Vector3.zero;
        Grenade.angularVelocity = Vector3.zero;
        Grenade.isKinematic = false;
        Grenade.freezeRotation = false;
        Grenade.transform.SetParent(null, true);

        Vector3 dir = grenadeForce * -GetComponentInParent<Transform>().forward;
        dir.y = granadeInclination;

        Debug.Log(dir);
        Grenade.AddForce(dir, ForceMode.Impulse);
    }

    [ServerRpc]
    public override IEnumerator WaitToDeleteServerRpc()
    {
        yield return new WaitForSeconds(5);

        Explode();

     //   GetComponent<Cinemachine.CinemachineImpulseSource>().GenerateImpulse(new Vector3(Random.Range(-1, 1), Random.Range(0.5f, 1), Random.Range(-1, 1)));

      /*  Grenade.freezeRotation = true;
        Grenade.isKinematic = true;
        Grenade.transform.SetParent(InitialParent, false);
        Grenade.rotation = InitialRotation;
        Grenade.transform.localPosition = InitialLocalPosition;
        IsGrenadeThrowAvailable = true;
      */
        Destroy(gameObject);
    }

    private void Explode()
    {
        GameObject particle = Instantiate(ExplosionParticleSystem, transform.position, Quaternion.identity);

        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider collider in colliders)
        {
            //Add force

            //Damage
            OnTriggerEnterServerRpc(collider);

        }

        DeleteObjectServerRcp(particle);
    }

    [ServerRpc]
    private IEnumerator DeleteObjectServerRcp(GameObject gameObjectToDelete)
    {
        yield return new WaitForSeconds(effectTime);

        Destroy(gameObjectToDelete);
    }









}
