using UnityEngine;

namespace Rayman
{
    public enum TangentMode
    {
        Linear,
        Auto,
        Broken
    }
    
    public class KnotProvider : MonoBehaviour
    {
        public TangentMode TangentMode = TangentMode.Auto;
        public Vector3 TangentIn;
        public Vector3 TangentOut;
        public float Radius = 0.01f;
        [HideInInspector] public int SplineIndex;
        [HideInInspector] public KnotProvider PreviousKnot;
        [HideInInspector] public KnotProvider NextKnot;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            Radius = Mathf.Max(0, Radius);
        }
#endif
    }
}
