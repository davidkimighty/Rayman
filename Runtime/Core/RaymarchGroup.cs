using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    [ExecuteInEditMode]
    public class RaymarchGroup : MonoBehaviour
    {
        [SerializeField] private Shader shader;
        [SerializeField] private List<RaymarchEntity> entities = new();
        [SerializeField] private List<RaymarchBufferProvider> bufferProviders = new();
        [SerializeField] private List<RaymarchDataProvider> dataProviders = new();
#if UNITY_EDITOR
        [Header("Debugging")]
        [SerializeField] private bool drawGizmos;
#endif
        
        private RaymarchEntity[] activeEntities;
        
        public bool IsInitialized => activeEntities != null;
        
        private void LateUpdate()
        {
            if (!IsInitialized) return;
            
            foreach (RaymarchBufferProvider provider in bufferProviders)
                provider.UpdateData();
        }
        
        public Material Setup()
        {
            activeEntities = entities.Where(s => s && s.gameObject.activeInHierarchy).ToArray();
            if (activeEntities.Length == 0) return null;

            Material matInstance = shader ? CoreUtils.CreateEngineMaterial(shader) :
                CoreUtils.CreateEngineMaterial("Universal Render Pipeline/Lit");

            foreach (RaymarchDataProvider provider in dataProviders)
                provider.SetData(ref matInstance);
            
            foreach (RaymarchBufferProvider provider in bufferProviders)
                provider.SetupBuffer(activeEntities, ref matInstance);
            return matInstance;
        }

        public void Release()
        {
            foreach (RaymarchBufferProvider provider in bufferProviders)
                provider.ReleaseBuffer();
            activeEntities = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (drawGizmos)
            {
                foreach (RaymarchBufferProvider provider in bufferProviders)
                    provider.DrawGizmos();
            }
        }
        
        [ContextMenu("Find all entities")]
        public void FindAllEntities()
        {
            entities = RaymarchUtils.GetChildrenByHierarchical<RaymarchEntity>(transform);
        }
        
        [ContextMenu("Find buffer providers")]
        public void FindAllBufferProviders()
        {
            bufferProviders = GetComponents<RaymarchBufferProvider>().ToList();
        }
#endif
    }
}
