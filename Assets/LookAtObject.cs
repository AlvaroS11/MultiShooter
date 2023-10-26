using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LookAtObject : NetworkBehaviour
{
    private Camera mainCamera;
    public Transform player;

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
        // Obt�n la rotaci�n de la c�mara
  //      Quaternion cameraRotation = mainCamera.rotation;

        // Aplica la rotaci�n de la c�mara a la barra de vida
        transform.rotation = mainCamera.transform.rotation;
        // transform.LookAt(transform.position + mainCamera.rotation * -Vector3.forward, mainCamera.rotation * Vector3.up);

        Vector3 playerScreenPos = mainCamera.WorldToScreenPoint(player.position);

        float verticalOffset = 40.0f; // Ajusta esto seg�n tus necesidades
        Vector3 newPivotPosition = new Vector3(playerScreenPos.x, playerScreenPos.y + verticalOffset, playerScreenPos.z);

        // Establece la nueva posici�n del objeto vac�o
        transform.position = mainCamera.ScreenToWorldPoint(newPivotPosition);

    }
}
