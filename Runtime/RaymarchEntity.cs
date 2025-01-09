using UnityEngine;

namespace Rayman
{
    public class RaymarchEntity : MonoBehaviour, IBoundsSource
    {
        public Vector3 Size = Vector3.one * 0.5f;
        public Vector3 Pivot = Vector3.one * 0.5f;
        public bool UseLossyScale = true;
        public float AdditionalExpandBounds;
        public float UpdateBoundsThreshold;
        
        public virtual T GetBounds<T>() where T : struct, IBounds<T>
        {
            return default;
        }
        
        public Vector3 GetScale() => UseLossyScale ? transform.lossyScale : Vector3.one;

#if UNITY_EDITOR
        private void OnValidate()
        {
            Pivot = new Vector3(
                Mathf.Clamp(Pivot.x, 0, 1),
                Mathf.Clamp(Pivot.y, 0, 1),
                Mathf.Clamp(Pivot.z, 0, 1));
        }
#endif
    }
}
