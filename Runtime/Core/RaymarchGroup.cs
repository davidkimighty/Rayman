using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    public class RaymarchGroup : MonoBehaviour
    {
        [HideInInspector] public Material MaterialInstance;
        
        [SerializeField] protected Material materialRef;
        [SerializeField] protected List<RaymarchBufferProvider> providers;
        [SerializeField] protected List<RaymarchEntity> entities = new();
        
        protected RaymarchEntity[] activeEntities;

        public void Setup()
        {
            activeEntities = entities.Where(s => s != null && s.gameObject.activeInHierarchy).ToArray();
            if (activeEntities.Length == 0) return;
            
            MaterialInstance = materialRef != null ? new Material(materialRef) :
                CoreUtils.CreateEngineMaterial("Universal Render Pipeline/Lit");
            
            foreach (RaymarchBufferProvider provider in providers)
                provider.Setup(ref MaterialInstance, activeEntities);
        }

        private void LateUpdate()
        {
            foreach (RaymarchBufferProvider provider in providers)
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
        }
        
#if UNITY_EDITOR
        [ContextMenu("Find all entities")]
        protected void FindAllEntities()
        {
            entities = RaymarchUtils.GetChildrenByHierarchical<RaymarchEntity>(transform);
        }
#endif
    }
}
