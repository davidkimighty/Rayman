using Rayman;
using UnityEngine;
using UnityEngine.InputSystem;

public class EyeLookAt : MonoBehaviour
{

    [SerializeField] private RaymarchShape eye;
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private float focusDst = 0.1f;
    [SerializeField] private InputActionReference mouseActionRef;

    private Quaternion startRotation;

    private void Start()
    {
        startRotation = eye.transform.rotation;
    }

    private void Update()
    {
        Vector3 mousePoint = mouseActionRef.action.ReadValue<Vector2>();
        mousePoint.z = Camera.main.nearClipPlane + focusDst;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(mousePoint);
        Vector3 dir = (mousePos - eye.transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(dir, transform.up);
        
        float angle = Quaternion.Angle(startRotation, targetRotation);
        eye.transform.rotation = angle > maxAngle
            ? Quaternion.RotateTowards(startRotation, targetRotation, maxAngle)
            : targetRotation;
    }
}
