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
        [SerializeField] private bool executeInEditMode;
        [SerializeField] private bool drawGizmos;
#endif

        private int groupId;
        private RaymarchEntity[] activeEntities;

        public bool IsInitialized => activeEntities != null;

        public Material Build()
        {
#if UNITY_EDITOR
            if (!executeInEditMode && !Application.isPlaying) return null;
#endif
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
#if UNITY_EDITOR
            if (!executeInEditMode && !Application.isPlaying) return;
#endif
            if (!IsInitialized) return;
            
            foreach (RaymarchDataProvider provider in providers)
                provider.SetData(groupId);
        }

        public void Release()
        {
#if UNITY_EDITOR
            if (!executeInEditMode && !Application.isPlaying) return;
#endif
            foreach (RaymarchDataProvider provider in providers)
                provider.Release(groupId);
            activeEntities = null;
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
