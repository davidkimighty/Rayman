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
        
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private int maxSteps = 64;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private int shadowMaxSteps = 32;
        [SerializeField] private float shadowMaxDistance = 30f;
        [SerializeField] private List<RaymarchGroup> raymarchGroups = new();

        public Material[] Materials => mainRenderer.materials;

        private void OnEnable()
        {
            Build();
        }
        
        private void OnDisable()
        {
            Release();
        }
        
        [ContextMenu("Build")]
        public void Build()
        {
            List<Material> matInstances = new();
            foreach (RaymarchGroup group in raymarchGroups)
            {
                Material mat = group.Build();
                if (mat == null) continue;
                
                matInstances.Add(mat);
                SetupRaymarchProperties(ref mat);
            }
            mainRenderer.materials = matInstances.ToArray();
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
        
        private void SetupRaymarchProperties(ref Material mat)
        {
            if (mat == null) return;
            
            mat.SetInt(MaxStepsId, maxSteps);
            mat.SetFloat(MaxDistanceId, maxDistance);
            mat.SetInt(ShadowMaxStepsId, shadowMaxSteps);
            mat.SetFloat(ShadowMaxDistanceId, shadowMaxDistance);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<Renderer>();
        }

        [ContextMenu("Find all groups")]
        private void FindAllEntities()
        {
            raymarchGroups = GetComponents<RaymarchGroup>().ToList();
            raymarchGroups.AddRange(RaymarchUtils.GetChildrenByHierarchical<RaymarchGroup>(transform));
        }
#endif
    }
}