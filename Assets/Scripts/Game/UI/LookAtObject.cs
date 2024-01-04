using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LookAtObject : NetworkBehaviour
{
    private Camera mainCamera;
    public Transform player;
    [SerializeField]
    private float verticalOffset = 2f;


    private void Start()
    {
        mainCamera = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        transform.rotation = mainCamera.transform.rotation;

        Vector3 playerScreenPos = mainCamera.WorldToScreenPoint(player.position);

        Vector3 newPivotPosition = new Vector3(playerScreenPos.x, playerScreenPos.y, playerScreenPos.z);

        // Establece la nueva posición del objeto vacío
        transform.position = mainCamera.ScreenToWorldPoint(newPivotPosition);
        transform.position = new Vector3(transform.position.x, verticalOffset, transform.position.z);
    }
}
