using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data/SSS")]
    public class SssData : ScriptableObject
    {
        public float f0 = 0.04f;
        public float roughness = 0.5f;
        public float sssDistortion = 0.1f;
        public float sssPower = 1f;
        public float sssScale = 0.5f;
        public float sssAmbient = 0.1f;
        public float sssThickness = 0.5f;
    }
}
