using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class Granade : Bullet
{
    [SerializeField]
    private int timeToExplode = 5;

    [SerializeField]
    private int radius = 5;


    [SerializeField]
    private GameObject ExplosionParticleSystem;

    public LayerMask floor;

    public LineRenderer lineRenderer;

    public int segments = 32;

    public float distToGround = - 0.34f;

    //public static DateTime previousTimeStamp = DateTime.Now.AddMilliseconds(-2000);

    public DateTime previousTimeStamp = DateTime.Now;

    public int actualNSegments = 0;

    private bool drawed;

    private bool exploded;

    private int lineIndex = 0;

    private int segmentsIncrease;

    [SerializeField]
    private Material[] lineMaterials;

    public AudioSource audioSource;


    //To Do add blink to granade
    public Material firstBlinkMaterial;
    public Material secondBlinkMaterial;


    void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(WaitToDeleteServerRpc());
        obstacleLayer = LayerMask.NameToLayer("Obstacle");
        playerLayer = LayerMask.NameToLayer("Player");
        bulletDmg = 20;

        playerManager = parent.GetComponent<PlayerManager>();

        lineRenderer.enabled = false;
        segmentsIncrease = segments / timeToExplode;
    }

    // Update is called once per frame
    void Update()
    {
       /// CalculateTimeToExplode();

        if (IsGrounded() && !drawed)
        {
            lineRenderer.enabled = true;
            //drawed = true;
            CalculateTimeToExplode();
        }
    }

    void CalculateTimeToExplode()
    {
        TimeSpan timeSinceLastUpdate = DateTime.Now - previousTimeStamp;

        if (timeSinceLastUpdate.TotalMilliseconds >= 666 && !exploded)
        {
            actualNSegments += segmentsIncrease;
            DrawExplosionArea();
            previousTimeStamp = DateTime.Now;
           // Debug.Break();
            if (lineIndex < lineMaterials.Length)
            {
                lineRenderer.material = lineMaterials[lineIndex];
                lineIndex++;
            }
                
            
        }

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
        yield return new WaitForSeconds(timeToExplode);

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
            OnTriggerEnterServerRpc(collider, false);

        }
        //drawed = true;
        exploded = true;
        lineRenderer.enabled = false;
        audioSource.Play();

        StartCoroutine(DeleteObjectServerRcp(timeToDestroy, particle));
    }

    [ServerRpc]
    private IEnumerator DeleteObjectServerRcp(int seconds, GameObject gameObjectToDelete)
    {
        yield return new WaitForSeconds(seconds);
        drawed = true;
        lineRenderer.enabled = false;
        Destroy(gameObjectToDelete);
        gameObjectToDelete.GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }

  private void DrawExplosionArea()
    {
        float x;
        float z;

        float angle = 360 / segments;

        lineRenderer.positionCount = actualNSegments;
        for (int i = 0; i < actualNSegments; i++)
        {
            x = transform.position.x + Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            z = transform.position.z + Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, 0, z));

            angle += (360f / segments);
        }
    }








}
