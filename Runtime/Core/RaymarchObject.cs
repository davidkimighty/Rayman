using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public class RaymarchObject : MonoBehaviour, IMaterialProvider
    {
        public event Action<IMaterialProvider> OnCreateMaterial;
        public event Action<IMaterialProvider> OnCleanupMaterial;
        
        [SerializeField] protected Shader shader;
        [SerializeField] protected List<MaterialDataProvider> materialDataProviders = new();

        protected Material material;

        public Material Material => material;

        protected virtual void OnDestroy()
        {
            Cleanup();
        }

        public virtual Material CreateMaterial()
        {
            if (material)
                Cleanup();
            material = new Material(shader);
            SetMaterialData();
            OnCreateMaterial?.Invoke(this);
            return material;
        }

        public virtual void Cleanup()
        {
            Destroy(material);
            OnCleanupMaterial?.Invoke(this);
        }

        protected virtual void SetMaterialData()
        {
            foreach (MaterialDataProvider provider in materialDataProviders)
                provider?.ProvideData(ref material);
        }
    }
}