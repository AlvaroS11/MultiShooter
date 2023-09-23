using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerControllerRayController : NetworkBehaviour
{
    private float speed = 3f;
    private Camera _mainCamera;
    private Vector3 _mouseInput;
    Vector3 mouseWorldCoordinates;
    Vector3 screenPosition;

    public LayerMask floor;

    void Start()
    {
        Initialized();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        //Initialized();
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
        if (!IsOwner || !Application.isFocused) return;
        //Movement
        screenPosition = Input.mousePosition;

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        
        if(Physics.Raycast(ray, out RaycastHit hitData,100, floor))
        {
            mouseWorldCoordinates = hitData.point;
            //  transform.position = mouseWorldCoordinates;
            transform.position = Vector3.MoveTowards(transform.position, mouseWorldCoordinates, Time.deltaTime * speed);

            if (mouseWorldCoordinates != transform.position)
            {
                Vector3 targetDirection = mouseWorldCoordinates - transform.position;
                transform.up = targetDirection;
            }
        }

    }

    private void OnMouseEnter()
    {
        print(mouseWorldCoordinates);
    }

}
