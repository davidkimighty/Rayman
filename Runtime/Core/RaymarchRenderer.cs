using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public class RaymarchRenderer : MonoBehaviour
    {
        public static readonly int EpsilonMinId = Shader.PropertyToID("_EpsilonMin");
        public static readonly int EpsilonMaxId = Shader.PropertyToID("_EpsilonMax");
        public static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        public static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");

        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private bool setupOnAwake = true;
        [SerializeField] private float epsilonMin = 0.001f;
        [SerializeField] private float epsilonMax = 0.01f;
        [SerializeField] private int maxSteps = 64;
        [SerializeField] private float maxRayDistance = 100f;
        [SerializeField] private List<MaterialDataProvider> materialDataProviders = new();
        [SerializeField] private List<RaymarchObject> raymarchObjects = new();

        private bool isReady;

        private void Awake()
        {
            if (setupOnAwake)
                Setup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<Renderer>();

            if (!isReady)
            {
                if (!Application.isPlaying)
                    Cleanup();
            }
        }
#endif

        [ContextMenu("Setup")]
        public void Setup()
        {
            if (raymarchObjects.Count == 0) return;

            if (isReady)
                Cleanup();

            List<Material> matInstances = new();
            foreach (RaymarchObject raymarchObject in raymarchObjects)
            {
                Material mat = raymarchObject?.CreateMaterial();
                if (!mat) continue;
                
                SetRaySettings(ref mat);
                matInstances.Add(mat);
            }
            mainRenderer.materials = matInstances.ToArray(); // has sort issue
            isReady = true;
        }

        [ContextMenu("Cleanup")]
        public void Cleanup()
        {
            foreach (RaymarchObject raymarchObject in raymarchObjects)
                raymarchObject?.Cleanup();

            if (mainRenderer)
                mainRenderer.materials = Array.Empty<Material>();
            isReady = false;
        }

        private void SetRaySettings(ref Material material)
        {
            material.SetFloat(EpsilonMinId, epsilonMin);
            material.SetFloat(EpsilonMaxId, epsilonMax);
            material.SetInt(MaxStepsId, maxSteps);
            material.SetFloat(MaxDistanceId, maxRayDistance);
            
            foreach (MaterialDataProvider provider in materialDataProviders)
                provider?.ProvideData(ref material);
        }
    }
}