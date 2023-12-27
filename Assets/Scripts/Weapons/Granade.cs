using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using static UnityEngine.ParticleSystem;


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

    public LayerMask floor;

    public LineRenderer lineRenderer;

    public int segments = 32;

    public float distToGround = 0f;

    private bool drawed; 
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
        if (IsGrounded() && !drawed)
            DrawExplosionArea();
    }

     private bool IsGrounded()
    {
        //return Physics.Raycast(transform.position, -Vector3.up, distToGround + 0.1);
        return transform.position.y <= distToGround;
    }

    public void ReleaseGrenade(float grenadeForce, float granadeInclination, Vector3? direction = null)
    {
        if (direction == null)
            direction = GetComponentInParent<Transform>().forward;
        else
        {
            direction = direction - transform.position;
        }
           //direction = Vector3.Normalize((Vector3)direction);



        Rigidbody Grenade = GetComponent<Rigidbody>();
        Grenade.velocity = Vector3.zero;
        Grenade.angularVelocity = Vector3.zero;
        Grenade.isKinematic = false;
        Grenade.freezeRotation = false;
        Grenade.transform.SetParent(null, true);

        //Vector3 dir = (Vector3)(grenadeForce * direction);
        //dir.y = granadeInclination;


        Vector3 newVel = Vector3.ClampMagnitude((Vector3)(direction / 2), grenadeForce);

        newVel.y += granadeInclination;



        Grenade.AddForce(newVel, ForceMode.Impulse);
        //        Grenade.AddForce(dir, ForceMode.Impulse);

    }


    public void ReleaseGrenadeMobile(float grenadeForce, float granadeInclination, Vector3 dir)
    {
        Rigidbody Grenade = GetComponent<Rigidbody>();
        Grenade.velocity = Vector3.zero;
        Grenade.angularVelocity = Vector3.zero;
        Grenade.isKinematic = false;
        Grenade.freezeRotation = false;
        Grenade.transform.SetParent(null, true);

        Vector3 direction = grenadeForce * dir;
        direction.y = granadeInclination;

        Grenade.AddForce(direction, ForceMode.Impulse);
    }


    [ServerRpc]
    public override IEnumerator WaitToDeleteServerRpc()
    {
        yield return new WaitForSeconds(5);

        Explode();
    }

    //Server only
    private void Explode()
    {
        GameObject particle = Instantiate(ExplosionParticleSystem, transform.position, Quaternion.identity);
        particle.GetComponent<NetworkObject>().Spawn();
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider collider in colliders)
        {
            //Add force

            //Damage
            OnTriggerEnterServerRpc(collider, false);

        }
        drawed = true;
        lineRenderer.enabled = false;

        StartCoroutine(DeleteObjectServerRcp(timeToDestroy, particle));
    }

    [ServerRpc]
    private IEnumerator DeleteObjectServerRcp(int seconds, GameObject gameObjectToDelete)
    {
        Debug.Log("Ienumerator");
        yield return new WaitForSeconds(seconds);
        drawed = true;
        lineRenderer.enabled = false;
        Debug.Log("DESTROYING " + gameObjectToDelete.name);
        Destroy(gameObjectToDelete);
        gameObjectToDelete.GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);



        //StartCoroutine(WaitToDeleteServerRpc());
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("trigger enter");
        GameObject hitObject = other.gameObject;
        if (hitObject.layer == floor)
        {
            Debug.Log("layer is floor");
            lineRenderer.enabled = true;
            DrawExplosionArea();
        }

    }


private void DrawExplosionArea()
    {
        Debug.Log(transform.position);
            float x;
            float y;
            float z;

            float angle = 360 / segments;

            lineRenderer.positionCount = segments;
            for (int i = 0; i < (segments + 1); i++)
            {
                x = transform.position.x + Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                z = transform.position.z + Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

                lineRenderer.SetPosition(i, new Vector3(x, 0, z));

                angle += (360f / segments);
            }
        drawed = true;
    }









}
