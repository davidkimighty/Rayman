using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data/SSS")]
    public class SssData : ScriptableObject
    {
        public float sssDistortion = 0.1f;
        public float sssPower = 1f;
        public float sssScale = 0.5f;
        public float sssAmbient = 0.1f;
        public float sssThickness = 0.5f;
    }
}
