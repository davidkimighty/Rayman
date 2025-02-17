using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class RaymarchRenderer : MonoBehaviour
    {
        public static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        public static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
        public static readonly int ShadowMaxStepsId = Shader.PropertyToID("_ShadowMaxSteps");
        public static readonly int ShadowMaxDistanceId = Shader.PropertyToID("_ShadowMaxDistance");

        public event Action<RaymarchRenderer> OnSetup;
        public event Action<RaymarchRenderer> OnRelease;

        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private bool setupOnAwake = true;
        [SerializeField] private int maxSteps = 64;
        [SerializeField] private float maxRayDistance = 100f;
        [SerializeField] private int shadowMaxSteps = 32;
        [SerializeField] private float shadowMaxRayDistance = 30f;
        [SerializeField] private List<RaymarchGroup> raymarchGroups = new();
        
        public bool IsInitialized  { get; private set; }
        public Material[] Materials => mainRenderer.materials;
        public List<RaymarchGroup> Groups => raymarchGroups;
        
        public int SdfCount => raymarchGroups.Sum(g => g.GetSdfCount());
        public int NodeCount => raymarchGroups.Sum(g => g.GetNodeCount());
        public int MaxHeight => raymarchGroups.Sum(g => g.GetMaxHeight());

        private void Awake()
        {
            if (setupOnAwake)
                Setup();
        }

        private void OnDestroy()
        {
            Release();
        }

        [ContextMenu("Setup")]
        public void Setup()
        {
            if (raymarchGroups.Count == 0) return;

            List<Material> matInstances = new();
            foreach (RaymarchGroup group in raymarchGroups)
            {
                Material mat = group.InitializeGroup();
                if (!mat) continue;
                
                SetupRaymarchProperties(ref mat);
                matInstances.Add(mat);
            }
            mainRenderer.materials = matInstances.ToArray();
            IsInitialized = true;
            OnSetup?.Invoke(this);
        }

        [ContextMenu("Release")]
        public void Release()
        {
            foreach (RaymarchGroup group in raymarchGroups)
                group?.ReleaseGroup();

            mainRenderer.materials = Array.Empty<Material>();
            IsInitialized = false;
            OnRelease?.Invoke(this);
        }

        private void SetupRaymarchProperties(ref Material mat)
        {
            if (!mat) return;

            mat.SetInt(MaxStepsId, maxSteps);
            mat.SetFloat(MaxDistanceId, maxRayDistance);
            mat.SetInt(ShadowMaxStepsId, shadowMaxSteps);
            mat.SetFloat(ShadowMaxDistanceId, shadowMaxRayDistance);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<Renderer>();

            if (!IsInitialized)
            {
                if (!Application.isPlaying)
                    Release();
            }
        }
        
        [ContextMenu("Find all groups")]
        public void FindAllGroups()
        {
            raymarchGroups = GetComponents<RaymarchGroup>().ToList();
            raymarchGroups.AddRange(RaymarchUtils.GetChildrenByHierarchical<RaymarchGroup>(transform));
        }
#endif
    }
}