using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class RaymarchRenderer : MonoBehaviour
    {
        public event Action<RaymarchRenderer> OnSetup;
        public event Action<RaymarchRenderer> OnCleanup;

        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private bool setupOnAwake = true;
        [SerializeField] private List<DataProvider> dataProviders = new();
        [SerializeField] private List<RaymarchObject> raymarchObjects = new();
        
        public bool IsReady  { get; private set; }
        public Material[] Materials => mainRenderer.materials;
        public List<RaymarchObject> RaymarchObjects => raymarchObjects;

        private void Awake()
        {
            if (setupOnAwake)
                Setup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        [ContextMenu("Setup")]
        public void Setup()
        {
            if (raymarchObjects.Count == 0) return;

            List<Material> matInstances = new();
            foreach (RaymarchObject raymarchObject in raymarchObjects)
            {
                Material mat = raymarchObject?.SetupMaterial();
                if (!mat) continue;
                
                foreach (DataProvider provider in dataProviders)
                    provider.ProvideData(ref mat);
                matInstances.Add(mat);
            }
            mainRenderer.materials = matInstances.ToArray();
            IsReady = true;
            OnSetup?.Invoke(this);
        }

        [ContextMenu("Cleanup")]
        public void Cleanup()
        {
            foreach (RaymarchObject raymarchObject in raymarchObjects)
                raymarchObject?.Cleanup();

            if (mainRenderer)
                mainRenderer.materials = Array.Empty<Material>();
            IsReady = false;
            OnCleanup?.Invoke(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<Renderer>();

            if (!IsReady)
            {
                if (!Application.isPlaying)
                    Cleanup();
            }
        }
        
        [ContextMenu("Find All Raymarch Objects")]
        public void FindAllRaymarchObjects()
        {
            raymarchObjects = GetComponents<RaymarchObject>().ToList();
            raymarchObjects.AddRange(RaymarchUtils.GetChildrenByHierarchical<RaymarchObject>(transform));
        }
#endif
    }
}