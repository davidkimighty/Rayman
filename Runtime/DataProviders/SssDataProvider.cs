using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Providers/SSS")]
    public class SssDataProvider : DataProvider
    {
        public static readonly int SssDistortionId = Shader.PropertyToID("_SssDistortion");
        public static readonly int SssPowerId = Shader.PropertyToID("_SssPower");
        public static readonly int SssScaleId = Shader.PropertyToID("_SssScale");
        public static readonly int SssAmbientId = Shader.PropertyToID("_SssAmbient");
        public static readonly int SssThicknessId = Shader.PropertyToID("_SssThickness");
        
        [SerializeField] private float sssDistortion = 0.1f;
        [SerializeField] private float sssPower = 1f;
        [SerializeField] private float sssScale = 0.5f;
        [SerializeField] private float sssAmbient = 0.1f;
        [SerializeField] private float sssThickness = 0.5f;
        
        public override void ProvideData(ref Material material)
        {
            material.SetFloat(SssDistortionId, sssDistortion);
            material.SetFloat(SssPowerId, sssPower);
            material.SetFloat(SssScaleId, sssScale);
            material.SetFloat(SssAmbientId, sssAmbient);
            material.SetFloat(SssThicknessId, sssThickness);
        }
    }
}
