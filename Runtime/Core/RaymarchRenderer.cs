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
        [SerializeField] private List<RaymarchObject> raymarchGroups = new();
        
        public bool IsInitialized  { get; private set; }
        public Material[] Materials => mainRenderer.materials;
        public List<RaymarchObject> Objects => raymarchGroups;

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
            foreach (RaymarchObject group in raymarchGroups)
            {
                Material mat = group.Initialize();
                if (!mat) continue;
                
                foreach (DataProvider provider in dataProviders)
                    provider?.ProvideData(ref mat);
                matInstances.Add(mat);
            }
            mainRenderer.materials = matInstances.ToArray();
            IsInitialized = true;
            OnSetup?.Invoke(this);
        }

        [ContextMenu("Release")]
        public void Release()
        {
            foreach (RaymarchObject group in raymarchGroups)
                group?.Release();

            if (mainRenderer)
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
        public void FindAllObjects()
        {
            raymarchGroups = GetComponents<RaymarchObject>().ToList();
            raymarchGroups.AddRange(RaymarchUtils.GetChildrenByHierarchical<RaymarchObject>(transform));
        }
#endif
    }
}