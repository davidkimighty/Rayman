using UnityEngine;

namespace Rayman
{
    public abstract class VisualProvider : MonoBehaviour
    {
        public bool IsDirty { get; set; } = true;
        
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            IsDirty = true;
        }
#endif
    }
}
