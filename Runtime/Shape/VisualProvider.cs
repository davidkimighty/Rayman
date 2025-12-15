using UnityEngine;

namespace Rayman
{
    public abstract class VisualProvider : MonoBehaviour
    {
        public bool IsVisualDirty { get; set; } = true;
        
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            IsVisualDirty = true;
        }
#endif
    }
}
