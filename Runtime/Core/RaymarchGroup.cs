using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayman
{
    [ExecuteInEditMode]
    public class RaymarchGroup : MonoBehaviour
    {
        [SerializeField] private Material materialRef;
        [SerializeField] private List<RaymarchDataProvider> providers = new();
        [SerializeField] private List<RaymarchEntity> entities = new();
#if UNITY_EDITOR
        [Header("Debugging")]
        [SerializeField] private bool drawGizmos;
#endif

        private int groupId;
        private RaymarchEntity[] activeEntities;

        public bool IsInitialized => activeEntities != null;

        public Material Build()
        {
            activeEntities = entities.Where(s => s != null && s.gameObject.activeInHierarchy).ToArray();
            if (activeEntities.Length == 0) return null;
            
            Material matInstance = materialRef != null ? new Material(materialRef) :
                CoreUtils.CreateEngineMaterial("Universal Render Pipeline/Lit");

            groupId = GetInstanceID();
            foreach (RaymarchDataProvider provider in providers)
                provider.Setup(groupId, activeEntities, ref matInstance);
            return matInstance;
        }

        private void LateUpdate()
        {
            if (!IsInitialized) return;
            
            foreach (RaymarchDataProvider provider in providers)
                provider.SetData(groupId);
        }

        public void Release()
        {
            foreach (RaymarchDataProvider provider in providers)
                provider.Release(groupId);
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawGizmos || !IsInitialized) return;
            
            foreach (RaymarchDataProvider provider in providers)
                provider.DrawGizmos(groupId);
        }

        [ContextMenu("Find all entities")]
        private void FindAllEntities()
        {
            entities = RaymarchUtils.GetChildrenByHierarchical<RaymarchEntity>(transform);
        }
#endif
    }
}
