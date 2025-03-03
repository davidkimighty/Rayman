using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public abstract class RaymarchObject : MonoBehaviour
    {
        public event Action<RaymarchObject> OnSetup;
        public event Action<RaymarchObject> OnRelease;
        
        [HideInInspector] public Material MatInstance;
        
        [SerializeField] protected Shader shader;
        [SerializeField] protected List<DataProvider> dataProviders = new();
        
        public abstract Material Initialize();
        public abstract void Release();
        
        public virtual bool IsInitialized() => MatInstance;

        protected virtual void ProvideShaderProperties()
        {
            foreach (DataProvider provider in dataProviders)
                provider?.ProvideData(ref MatInstance);
        }

        protected void InvokeOnSetup() => OnSetup?.Invoke(this);
        
        protected void InvokeOnRelease() => OnRelease?.Invoke(this);
    }
}
