using System;
using Rayman;
using UnityEngine;
using UnityEngine.InputSystem;

public class EyeLookAt : MonoBehaviour
{

    [SerializeField] private RaymarchShape eye;
    [SerializeField] private Transform eyeDir;
    [SerializeField] private float eyeRotAngle;
    [SerializeField] private InputActionReference mouseActionRef;

    private void Update()
    {
        Vector3 mousePoint = mouseActionRef.action.ReadValue<Vector2>();
        mousePoint.z = Camera.main.nearClipPlane;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(mousePoint);
        Vector3 dir = (mousePos - eye.transform.position).normalized;
        eye.transform.forward = dir;
    }
}
