using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/Debug Data Provider")]
    public class DebugDataProvider : RaymarchMaterialDataProvider
    {
        private static readonly int DebugModeId = Shader.PropertyToID("_DebugMode");
        private static readonly int BoundsDisplayThresholdId = Shader.PropertyToID("_BoundsDisplayThreshold");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int CullId = Shader.PropertyToID("_Cull");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        
        [SerializeField] private DebugModes debugMode;
        [SerializeField] private int boundsDisplayThreshold = 300;
        [SerializeField] private BlendMode srcBlend = BlendMode.One;
        [SerializeField] private BlendMode dstBlend = BlendMode.Zero;
        [SerializeField] private CullMode cull = CullMode.Back;
        [SerializeField] private bool zWrite = true;

        public override void SetData(ref Material mat)
        {
            mat.SetInt(DebugModeId, (int)debugMode);
            mat.SetInt(BoundsDisplayThresholdId, boundsDisplayThreshold);
            mat.SetFloat(SrcBlendId, (float)srcBlend);
            mat.SetFloat(DstBlendId, (float)dstBlend);
            mat.SetInt(CullId, (int)cull);
            mat.SetFloat(ZWriteId, zWrite ? 1f : 0f);
        }
    }
}
