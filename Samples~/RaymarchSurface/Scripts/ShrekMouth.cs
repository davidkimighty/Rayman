using Rayman;
using UnityEngine;

public class ShrekMouth : MonoBehaviour
{
    [SerializeField] private RaymarchShape mouth;
    [SerializeField] private Vector3 startShape;
    [SerializeField] private Vector3 endShape;
    [SerializeField] private float frequency = 1f;
    [SerializeField] private AnimationCurve curve;

    private void Update()
    {
        float s = Mathf.Sin(Time.time * frequency * Mathf.PI);
        mouth.Settings.Size = Vector3.LerpUnclamped(startShape, endShape, curve.Evaluate(s));
    }
}
