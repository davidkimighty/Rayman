using UnityEngine;

namespace Rayman
{
    public class KnotProvider : MonoBehaviour
    {
        public Vector3 TangentIn;
        public Vector3 TangentOut;
        public float Radius = 0.01f;
        public float Blend = 0.001f;
        [HideInInspector] public int SplineIndex;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            Radius = Mathf.Max(0, Radius);
        }
#endif
    }
}
