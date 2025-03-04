using System.Collections;
using System.Collections.Generic;
using Rayman;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractiveShapes : MonoBehaviour
{
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    
    [SerializeField] private RaymarchRenderer raymarchRenderer;
    [SerializeField] private InputActionReference clickActionReference;
    [SerializeField] private List<Texture> matcaps;
    
    [SerializeField] private ShapeElement shapePrimary;
    [SerializeField] private float interactSize;
    [SerializeField] private float interactDuration;
    [SerializeField] private AnimationCurve interactCurve;
    
    [SerializeField] private ShapeElement shapeSecondary;
    [SerializeField] private Vector2 sizeMinMax;
    [SerializeField] private float pulseFrequency;

    private int matcapIndex;
    private Vector3 primaryOriginalSize;
    private IEnumerator pulseImpact;

    private void OnEnable()
    {
        clickActionReference.action.performed += ChangeMatCap;

        primaryOriginalSize = shapePrimary.Size;
    }
    
    private void OnDisable()
    {
        clickActionReference.action.performed -= ChangeMatCap;
    }

    private void Update()
    {
        ContinuousPulse(shapeSecondary, pulseFrequency, sizeMinMax);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (pulseImpact != null)
            StopCoroutine(pulseImpact);
        pulseImpact = PulseImpact(shapePrimary, Vector3.one * interactSize, interactDuration);
        StartCoroutine(pulseImpact);
    }

    private void OnTriggerExit(Collider other)
    {
        if (pulseImpact != null)
            StopCoroutine(pulseImpact);
        pulseImpact = PulseImpact(shapePrimary, primaryOriginalSize, interactDuration);
        StartCoroutine(pulseImpact);
    }

    public IEnumerator PulseImpact(ShapeElement shape, Vector3 targetSize, float duration)
    {
        float elapsedTime = 0f;
        Vector3 startSize = shape.Size;
        
        while (elapsedTime < duration)
        {
            float lerpFactor = interactCurve.Evaluate(elapsedTime / duration);
            shape.Size = Vector3.LerpUnclamped(startSize, targetSize, lerpFactor);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        shape.Size = targetSize;
    }

    private void ContinuousPulse(ShapeElement shape, float frequency, Vector2 minmax)
    {
        float sin = (Mathf.Sin(Time.time * frequency) + 1f) / 2f;
        float size = Mathf.Lerp(minmax.x, minmax.y, sin);
        shape.Size = new Vector3(size, 0, 0);
    }

    private void ChangeMatCap(InputAction.CallbackContext context)
    {
        bool click = context.ReadValueAsButton();
        if (!click) return;
        
        matcapIndex = ++matcapIndex % matcaps.Count;
        raymarchRenderer.Materials[0].SetTexture(MainTexId, matcaps[matcapIndex]);
    }
}
