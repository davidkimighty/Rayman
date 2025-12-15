using System.Collections;
using System.Collections.Generic;
using Rayman;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractiveShapes : MonoBehaviour
{
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    
    [SerializeField] private RaymarchObject raymarchObject;
    [SerializeField] private InputActionReference clickActionReference;
    [SerializeField] private List<Texture> matcaps;
    
    [SerializeField] private ShapeProvider shapePrimary;
    [SerializeField] private float interactSize;
    [SerializeField] private float interactDuration;
    [SerializeField] private AnimationCurve interactCurve;
    
    [SerializeField] private ShapeProvider shapeSecondary;
    [SerializeField] private Vector2 sizeMinMax;
    [SerializeField] private float pulseFrequency;

    private int matcapIndex;
    private IEnumerator pulseImpact;

    private void OnEnable()
    {
        clickActionReference.action.performed += ChangeMatCap;

        raymarchObject.Material.SetTexture(BaseMapId, matcaps[0]);
    }
    
    private void OnDisable()
    {
        clickActionReference.action.performed -= ChangeMatCap;
    }

    private void Update()
    {
        ContinuousPulse(shapeSecondary, pulseFrequency, sizeMinMax);
    }
    
    private void ContinuousPulse(ShapeProvider shape, float frequency, Vector2 minmax)
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
        raymarchObject.Material.SetTexture(BaseMapId, matcaps[matcapIndex]);
    }
}
