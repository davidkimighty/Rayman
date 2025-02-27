using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class RaymarchRenderer : MonoBehaviour
    {
        public event Action<RaymarchRenderer> OnSetup;
        public event Action<RaymarchRenderer> OnRelease;

        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private bool setupOnAwake = true;
        [SerializeField] private List<DataProvider> dataProviders = new();
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
                
                foreach (DataProvider provider in dataProviders)
                    provider?.ProvideShaderProperties(ref mat);
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