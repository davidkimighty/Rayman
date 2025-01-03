using Rayman;
using UnityEngine;

public class RaymarchSizePulse : MonoBehaviour
{
    [SerializeField] private RaymarchShape shape;
    [SerializeField] private Vector2 sizeMinMax;
    [SerializeField] private float pulseFrequency;
    
    private void Update()
    {
        float sin = (Mathf.Sin(Time.time * pulseFrequency) + 1f) / 2f;
        float size = Mathf.Lerp(sizeMinMax.x, sizeMinMax.y, sin);
        shape.Settings.Size = new Vector3(size, 0, 0);
    }
}
