using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class RaymarchRenderer : MonoBehaviour
    {
        public static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        public static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
        public static readonly int ShadowMaxStepsId = Shader.PropertyToID("_ShadowMaxSteps");
        public static readonly int ShadowMaxDistanceId = Shader.PropertyToID("_ShadowMaxDistance");

        public event Action<RaymarchRenderer> OnBuild;
        public event Action<RaymarchRenderer> OnRelease;
        
        [SerializeField] protected Renderer mainRenderer;
        [SerializeField] protected int maxSteps = 64;
        [SerializeField] protected float maxDistance = 100f;
        [SerializeField] protected int shadowMaxSteps = 32;
        [SerializeField] protected float shadowMaxDistance = 30f;
        [SerializeField] protected List<RaymarchGroup> raymarchGroups = new();
#if UNITY_EDITOR
        [Header("Debugging")]
        [SerializeField] protected bool executeInEditor;
#endif

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !executeInEditor) return;
#endif
            Build();
        }
        
        protected virtual void OnDisable()
        {
            Release();
        }
        
        [ContextMenu("Build")]
        public void Build()
        {
            foreach (RaymarchGroup group in raymarchGroups)
            {
                group.Build();
                SetupRaymarchProperties(ref group.MaterialInstance);
            }
            mainRenderer.materials = raymarchGroups.Select(g => g.MaterialInstance).ToArray();
            OnBuild?.Invoke(this);
        }

        [ContextMenu("Release")]
        public void Release()
        {
            foreach (RaymarchGroup group in raymarchGroups)
                group?.Release();
            mainRenderer.materials = Array.Empty<Material>();
            OnRelease?.Invoke(this);
        }
        
        protected void SetupRaymarchProperties(ref Material mat)
        {
            if (mat == null) return;
            
            mat.SetInt(MaxStepsId, maxSteps);
            mat.SetFloat(MaxDistanceId, maxDistance);
            mat.SetInt(ShadowMaxStepsId, shadowMaxSteps);
            mat.SetFloat(ShadowMaxDistanceId, shadowMaxDistance);
        }
        
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<Renderer>();
            
            if (executeInEditor)
            {
                foreach (RaymarchGroup group in raymarchGroups)
                {
                    if (!group.IsInitialized) continue;
                    
                    SetupRaymarchProperties(ref group.MaterialInstance);
                }
            }
        }

        [ContextMenu("Find all groups")]
        protected void FindAllEntities()
        {
            raymarchGroups = RaymarchUtils.GetChildrenByHierarchical<RaymarchGroup>(transform);
        }
#endif
    }
}