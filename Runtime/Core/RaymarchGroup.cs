using System;
using UnityEngine;

namespace Rayman
{
    public abstract class RaymarchGroup : MonoBehaviour
    {
        public event Action<RaymarchGroup> OnSetup;
        public event Action<RaymarchGroup> OnRelease;
        
        [HideInInspector] public Material MatInstance;
        
        [SerializeField] protected Shader shader;
#if UNITY_EDITOR
        [SerializeField] protected bool drawGizmos;
#endif
        
        public abstract Material InitializeGroup();
        public abstract void ReleaseGroup();
        
        public virtual bool IsInitialized() => MatInstance != null;
        
        public virtual void SetupShaderProperties(ref Material material) { }

        protected void InvokeOnSetup() => OnSetup?.Invoke(this);
        
        protected void InvokeOnRelease() => OnRelease?.Invoke(this);
    }
}
