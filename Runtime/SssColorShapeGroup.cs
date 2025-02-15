using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class SssColorShapeGroup : ColorShapeGroup
    {
        private static readonly int SssDistortionId = Shader.PropertyToID("_SssDistortion");
        private static readonly int SssPowerId = Shader.PropertyToID("_SssPower");
        private static readonly int SssScaleId = Shader.PropertyToID("_SssScale");
        private static readonly int SssAmbientId = Shader.PropertyToID("_SssAmbient");
        private static readonly int SssThicknessId = Shader.PropertyToID("_SssThickness");
        
        [Header("SSS")]
        [SerializeField] private SssData sssData;

        public override void SetupShaderProperties(ref Material material)
        {
            base.SetupShaderProperties(ref material);
            
            material.SetFloat(SssDistortionId, sssData.sssDistortion);
            material.SetFloat(SssPowerId, sssData.sssPower);
            material.SetFloat(SssScaleId, sssData.sssScale);
            material.SetFloat(SssAmbientId, sssData.sssAmbient);
            material.SetFloat(SssThicknessId, sssData.sssThickness);
        }
    }
}
