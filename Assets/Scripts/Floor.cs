using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floor : MonoBehaviour
{
    private Vector3 _mouseInput;
    Vector3 mouseWorldCoordinates;
    private Camera _mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        _mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseDown()
    {
        Vector3 mousePosition = (Vector3)Input.mousePosition;
      //  _mouseInput.x = mousePosition.x;
      //  _mouseInput.y = Camera.main.nearClipPlane + 16;
      //  _mouseInput.z = mousePosition.y;

        print(mousePosition);

        mouseWorldCoordinates = _mainCamera.ScreenToWorldPoint(_mouseInput);
        mouseWorldCoordinates.y = 0;
     //   print(mouseWorldCoordinates);
    }
}
