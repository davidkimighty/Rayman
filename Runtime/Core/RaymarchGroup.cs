using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    [ExecuteInEditMode]
    public class RaymarchGroup : MonoBehaviour
    {
        [HideInInspector] public Material MaterialInstance;
        
        [SerializeField] protected Material materialRef;
        [SerializeField] protected List<RaymarchDataProvider> providers = new();
        [SerializeField] protected List<RaymarchEntity> entities = new();
        
        protected RaymarchEntity[] activeEntities;

        public bool IsInitialized => activeEntities != null;

        public void Build()
        {
            activeEntities = entities.Where(s => s != null && s.gameObject.activeInHierarchy).ToArray();
            if (activeEntities.Length == 0) return;
            
            MaterialInstance = materialRef != null ? new Material(materialRef) :
                CoreUtils.CreateEngineMaterial("Universal Render Pipeline/Lit");
            
            foreach (RaymarchDataProvider provider in providers)
                provider.Setup(ref MaterialInstance, activeEntities);
        }

        private void LateUpdate()
        {
            if (!IsInitialized) return;
            
            foreach (RaymarchDataProvider provider in providers)
                provider.SetData();
        }

        public void Release()
        {
            if (MaterialInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(MaterialInstance);
                else
                    DestroyImmediate(MaterialInstance);
                MaterialInstance = null;
            }
            
            foreach (RaymarchDataProvider provider in providers)
                provider.Release();
        }
        
#if UNITY_EDITOR
        [ContextMenu("Find all entities")]
        protected void FindAllEntities()
        {
            entities = RaymarchUtils.GetChildrenByHierarchical<RaymarchEntity>(transform);
        }
        
        [ContextMenu("Find all providers")]
        protected void FindAllProviders()
        {
            providers = GetComponents<RaymarchDataProvider>().ToList();
        }
#endif
    }
}
