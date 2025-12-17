using Rayman;
using UnityEngine;

[ExecuteInEditMode]
public class MengerSponge : RaymarchObject
{
    public float Size = 0.5f;
    [Range(1, 10)] public int Iterations = 4;
    public float Scale = 3.0f;
    public float ScaleMultiplier = 4.0f;

    private void LateUpdate()
    {
        if (!material) return;

        UpdateData();
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!material) return;

        UpdateData();
    }
#endif
    
    public override Material CreateMaterial()
    {
        material = new Material(shader);
        if (!material) return null;

        UpdateData();
        return material;
    }

    public override void Cleanup()
    {
        if (Application.isEditor)
            DestroyImmediate(material);
        else
            Destroy(material);
    }

    private void UpdateData()
    {
        material.SetMatrix("_Transform", transform.worldToLocalMatrix);
        material.SetFloat("_Size", Size);
        material.SetInt("_Iterations", Iterations);
        material.SetFloat("_Scale", Scale);
        material.SetFloat("_ScaleMultiplier", ScaleMultiplier);
    }
}
