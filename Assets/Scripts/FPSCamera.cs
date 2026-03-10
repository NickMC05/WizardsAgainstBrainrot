using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCamera : MonoBehaviour
{
    public float MouseX;
    public float MouseY;
    public Transform Body;
    public Transform Head;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MouseX = Input.GetAxis("Mouse X") * 100 * Time.deltaTime;
        Body.Rotate(Vector3.up, MouseX);

        MouseY = Input.GetAxis("Mouse Y") * 100 * Time.deltaTime;
        Head.Rotate(Vector3.left, MouseY);
    }
}
