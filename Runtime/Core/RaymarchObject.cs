using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public class RaymarchObject : MonoBehaviour, IMaterialProvider
    {
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
            return material;
        }

        public virtual void Cleanup()
        {
            Destroy(material);
        }

        public virtual void Refresh()
        {
            if (!material) return;
            
            SetMaterialData();
        }

        protected virtual void SetMaterialData()
        {
            foreach (MaterialDataProvider provider in materialDataProviders)
                provider?.ProvideData(ref material);
        }
    }
}