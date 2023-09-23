using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    private float speed = 3f;
    private Camera _mainCamera;
    private Vector3 _mouseInput;
    Vector3 mouseWorldCoordinates;
    Vector3 screenPosition;


    void Start()
    {
        Initialized();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Initialized();
    }

    private void Initialized()
    {
        print("MAIN CAMERA GOT");
       _mainCamera = Camera.main;
        print(_mainCamera.transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        if (!Application.isFocused) return;
        //Movement
        screenPosition = Input.mousePosition;
        screenPosition.z = Camera.main.nearClipPlane + 16;

        mouseWorldCoordinates = Camera.main.ScreenToWorldPoint(screenPosition);

        Vector3 playerDesiredMovement = new Vector3(mouseWorldCoordinates.x, mouseWorldCoordinates.y);

        transform.position = Vector3.MoveTowards(transform.position, playerDesiredMovement, Time.deltaTime * speed);

        if (mouseWorldCoordinates != transform.position)
        {
            Vector3 targetDirection = playerDesiredMovement - transform.position;
            transform.up = targetDirection;
        }
    }

    private void OnMouseEnter()
    {
        print(mouseWorldCoordinates);
    }

}
