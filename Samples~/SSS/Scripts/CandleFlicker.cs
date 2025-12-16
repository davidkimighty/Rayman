using UnityEngine;

public class CandleFlicker : MonoBehaviour
{
    [SerializeField] private Light candleLight;
    [SerializeField] private float minIntensity = 0.8f;
    [SerializeField] private float maxIntensity = 1.2f;
    [SerializeField] private float flickerSpeed = 0.1f;
    [SerializeField] private float smoothing = 0.05f;
    
    private float targetIntensity;
    private float currentVelocity;

    private void Start()
    {
        targetIntensity = candleLight.intensity;
    }

    private void Update()
    {
        if (Time.time % flickerSpeed < Time.deltaTime)
            targetIntensity = Random.Range(minIntensity, maxIntensity);

        candleLight.intensity = Mathf.SmoothDamp(candleLight.intensity, targetIntensity, ref currentVelocity, smoothing);
    }
}