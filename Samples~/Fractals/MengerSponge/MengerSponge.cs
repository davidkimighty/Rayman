using UnityEngine;

[ExecuteInEditMode]
public class MengerSponge : MonoBehaviour
{
    public Renderer mainRenderer;
    public Shader shader;
    public float Size = 0.5f;
    [Range(1, 10)] public int Iterations = 4;
    public float Scale = 3.0f;
    public float ScaleMultiplier = 4.0f;

    private Material material;

    private void Awake()
    {
        SetupMaterial();
    }

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
    
    public void SetupMaterial()
    {
        material = new Material(shader);
        mainRenderer.material = material;
        UpdateData();
    }

    public void CleanupMaterial()
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
