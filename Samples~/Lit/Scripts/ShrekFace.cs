using Rayman;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShrekFace : MonoBehaviour
{
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private float focusDst = 0.1f;
    [SerializeField] private InputActionReference mouseActionRef;
    
    [SerializeField] private RaymarchShapeEntity mouth;
    [SerializeField] private Vector3 startShape;
    [SerializeField] private Vector3 endShape;
    [SerializeField] private float frequency = 1f;
    [SerializeField] private AnimationCurve curve;

    private Quaternion leftEyeStartRotation;
    private Quaternion rightEyeStartRotation;
    
    private void Update()
    {
        float s = Mathf.Sin(Time.time * frequency * Mathf.PI);
        mouth.Size = Vector3.LerpUnclamped(startShape, endShape, curve.Evaluate(s));
    }

    private void LookAt(Transform eye, Vector3 target, Quaternion startRot)
    {
        Vector3 dir = (target - eye.transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(dir, transform.up);
        
        float angle = Quaternion.Angle(startRot, targetRotation);
        eye.transform.rotation = angle > maxAngle
            ? Quaternion.RotateTowards(startRot, targetRotation, maxAngle)
            : targetRotation;
    }
}
