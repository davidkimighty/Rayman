using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/SSS Data Provider")]
    public class SssDataProvider : RaymarchDataProvider
    {
        private static readonly int F0Id = Shader.PropertyToID("_F0");
        private static readonly int SssDistortionId = Shader.PropertyToID("_SssDistortion");
        private static readonly int SssPowerId = Shader.PropertyToID("_SssPower");
        private static readonly int SssScaleId = Shader.PropertyToID("_SssScale");
        private static readonly int SssAmbientId = Shader.PropertyToID("_SssAmbient");
        private static readonly int SssThicknessId = Shader.PropertyToID("_SssThickness");

        [SerializeField] private float f0 = 0.04f;
        [SerializeField] private float sssDistortion = 0.1f;
        [SerializeField] private float sssPower = 1f;
        [SerializeField] private float sssScale = 0.5f;
        [SerializeField] private float sssAmbient = 0.1f;
        [SerializeField] private float sssThickness = 0.5f;
        
        public override void SetData(ref Material mat)
        {
            mat.SetFloat(F0Id, f0);
            mat.SetFloat(SssDistortionId, sssDistortion);
            mat.SetFloat(SssPowerId, sssPower);
            mat.SetFloat(SssScaleId, sssScale);
            mat.SetFloat(SssAmbientId, sssAmbient);
            mat.SetFloat(SssThicknessId, sssThickness);
        }
    }
}
