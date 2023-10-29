using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : Weapon
{
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();   
    }

    protected override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
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

}
