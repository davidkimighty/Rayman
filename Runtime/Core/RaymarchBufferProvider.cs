using System;
using UnityEngine;

namespace Rayman
{
    public abstract class RaymarchBufferProvider : MonoBehaviour
    {
        public event Action<RaymarchBufferProvider> OnSetup;
        public event Action<RaymarchBufferProvider> OnRelease;
        
        protected GraphicsBuffer buffer;
        
        public bool IsInitialized => buffer != null;
        
        public abstract void SetupBuffer(RaymarchEntity[] entities, ref Material mat);
        public abstract void UpdateData();
        public abstract void ReleaseBuffer();
        
#if UNITY_EDITOR
        public virtual void DrawGizmos(){ }
#endif

        protected virtual void InvokeOnSetup() => OnSetup?.Invoke(this);
        protected virtual void InvokeOnRelease() => OnRelease?.Invoke(this);
    }
}
