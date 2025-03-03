using Rayman;
using UnityEngine;

[ExecuteInEditMode]
public class MengerSponge : RaymarchGroup
{
    public float Size = 0.5f;
    [Range(1, 10)] public int Iterations = 4;
    public float Scale = 3.0f;
    public float ScaleMultiplier = 4.0f;
    
    private void LateUpdate()
    {
        if (!IsInitialized()) return;

        UpdateData();
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!IsInitialized()) return;

        ProvideShaderProperties();
    }
#endif
    
    public override Material InitializeGroup()
    {
        MatInstance = new Material(shader);
        if (!MatInstance) return null;
            
        ProvideShaderProperties();
        InvokeOnSetup();
        return MatInstance;
    }

    public override void ReleaseGroup()
    {
        if (Application.isEditor)
            DestroyImmediate(MatInstance);
        else
            Destroy(MatInstance);
        InvokeOnRelease();
    }

    protected override void ProvideShaderProperties()
    {
        base.ProvideShaderProperties();
        UpdateData();
    }

    private void UpdateData()
    {
        MatInstance.SetMatrix("_Transform", transform.worldToLocalMatrix);
        MatInstance.SetFloat("_Size", Size);
        MatInstance.SetInt("_Iterations", Iterations);
        MatInstance.SetFloat("_Scale", Scale);
        MatInstance.SetFloat("_ScaleMultiplier", ScaleMultiplier);
    }
}
