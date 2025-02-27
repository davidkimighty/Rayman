using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/Render State")]
    public class RenderStateDataProvider : DataProvider
    {
        public static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        public static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        public static readonly int CullId = Shader.PropertyToID("_Cull");
        public static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        
        public BlendMode SrcBlend = BlendMode.One;
        public BlendMode DstBlend = BlendMode.Zero;
        public CullMode Cull = CullMode.Back;
        public bool ZWrite = true;
        
        public override void ProvideShaderProperties(ref Material material)
        {
            material.SetFloat(SrcBlendId, (float)SrcBlend);
            material.SetFloat(DstBlendId, (float)DstBlend);
            material.SetInt(CullId, (int)Cull);
            material.SetFloat(ZWriteId, ZWrite ? 1f : 0f);
        }
    }
}
