using System.Collections;
using UnityEngine;

public class LerpSponge : MonoBehaviour
{
    [SerializeField] private MengerSponge sponge;
    [SerializeField] private float minValue;
    [SerializeField] private float maxValue;
    [SerializeField] private float duration;
    [SerializeField] private AnimationCurve curve;

    private IEnumerator Start()
    {
        sponge.Size = minValue;
        float elapsedTime = 0;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = curve.Evaluate(elapsedTime / duration);
            sponge.Size = Mathf.Lerp(minValue, maxValue, t);
            yield return null;
        }
        sponge.Size = maxValue;
    }
}
