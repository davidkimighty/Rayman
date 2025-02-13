using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/Blend Mode Data Provider")]
    public class BlendModeDataProvider : RaymarchMaterialDataProvider
    {
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int CullId = Shader.PropertyToID("_Cull");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        
        [SerializeField] private BlendMode srcBlend = BlendMode.One;
        [SerializeField] private BlendMode dstBlend = BlendMode.Zero;
        [SerializeField] private CullMode cullMode = CullMode.Back;
        [SerializeField] private bool zWrite = true;
        
        public override void SetData(ref Material mat)
        {
            mat.SetFloat(SrcBlendId, (float)srcBlend);
            mat.SetFloat(DstBlendId, (float)dstBlend);
            mat.SetInt(CullId, (int)cullMode);
            mat.SetFloat(ZWriteId, zWrite ? 1f : 0f);
        }
    }
}
