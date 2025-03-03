using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public abstract class RaymarchGroup : MonoBehaviour
    {
        public event Action<RaymarchGroup> OnSetup;
        public event Action<RaymarchGroup> OnRelease;
        
        [HideInInspector] public Material MatInstance;
        
        [SerializeField] protected Shader shader;
        [SerializeField] protected List<DataProvider> dataProviders = new();
        
        public abstract Material InitializeGroup();
        public abstract void ReleaseGroup();
        
        public virtual bool IsInitialized() => MatInstance != null;

        protected virtual void ProvideShaderProperties()
        {
            foreach (DataProvider provider in dataProviders)
                provider?.ProvideData(ref MatInstance);
        }

        protected void InvokeOnSetup() => OnSetup?.Invoke(this);
        
        protected void InvokeOnRelease() => OnRelease?.Invoke(this);
    }
}
