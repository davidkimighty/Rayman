using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class RaymarchRenderer : MonoBehaviour
    {
        public event Action<RaymarchRenderer> OnInitialize;
        public event Action<RaymarchRenderer> OnRelease;

        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private bool setupOnAwake = true;
        [SerializeField] private List<DataProvider> dataProviders = new();
        [SerializeField] private List<RaymarchObject> raymarchObjects = new();
        
        public bool IsInitialized  { get; private set; }
        public Material[] Materials => mainRenderer.materials;
        public List<RaymarchObject> RaymarchObjects => raymarchObjects;

        private void Awake()
        {
            if (setupOnAwake)
                Initialize();
        }

        private void OnDestroy()
        {
            Release();
        }

        [ContextMenu("Initialize")]
        public void Initialize()
        {
            if (raymarchObjects.Count == 0) return;

            List<Material> matInstances = new();
            foreach (RaymarchObject ro in raymarchObjects)
            {
                Material mat = ro.Initialize();
                if (!mat) continue;
                
                foreach (DataProvider provider in dataProviders)
                    provider?.ProvideData(ref mat);
                matInstances.Add(mat);
            }
            mainRenderer.materials = matInstances.ToArray();
            IsInitialized = true;
            OnInitialize?.Invoke(this);
        }

        [ContextMenu("Release")]
        public void Release()
        {
            foreach (RaymarchObject ro in raymarchObjects)
                ro?.Release();

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
        
        [ContextMenu("Find All Raymarch Objects")]
        public void FindAllRaymarchObjects()
        {
            raymarchObjects = GetComponents<RaymarchObject>().ToList();
            raymarchObjects.AddRange(RaymarchUtils.GetChildrenByHierarchical<RaymarchObject>(transform));
        }
#endif
    }
}