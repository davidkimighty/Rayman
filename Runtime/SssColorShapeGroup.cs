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

        protected override void SetupShaderProperties(ref Material mat)
        {
            base.SetupShaderProperties(ref mat);
            mat.SetFloat(SssDistortionId, sssData.sssDistortion);
            mat.SetFloat(SssPowerId, sssData.sssPower);
            mat.SetFloat(SssScaleId, sssData.sssScale);
            mat.SetFloat(SssAmbientId, sssData.sssAmbient);
            mat.SetFloat(SssThicknessId, sssData.sssThickness);
        }
    }
}
