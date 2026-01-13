using System;
using System.Collections.Generic;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ShapeObject : MonoBehaviour
    {
        [SerializeField] private bool setupOnAwake = true;
        [SerializeField] private bool drawGizmos;
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private RayDataProvider rayDataProvider;
        [SerializeField] private Shader shader;
        [SerializeField] private List<MaterialDataProvider> materialDataProviders = new();
        [SerializeField] private List<ShapeProvider> shapeProviders;

        private Material material;

        private BvhBufferProvider nodeBufferProvider;
        private ShapeBufferProvider shapeBufferProvider;
        private ColorBufferProvider colorBufferProvider;

        private NativeArray<Aabb> leafBounds;
        private ShapeData[] shapeData;
        private ColorData[] colorData;

        public bool IsInitialized => material && nodeBufferProvider != null;
        public Material Material => material;
        public int ShapeCount => shapeProviders.Count;
        public int NodeCount => IsInitialized ? nodeBufferProvider.DataLength : 0;

        private void Awake()
        {
            if (setupOnAwake)
                SetupMaterial();
        }

        private void LateUpdate()
        {
            if (!IsInitialized) return;

            UpdateBufferData();

            // isDirty checks?
            nodeBufferProvider.SetData(leafBounds);
            shapeBufferProvider.SetData(shapeData);
            colorBufferProvider.SetData(colorData);
        }

        private void OnDestroy()
        {
            CleanupMaterial();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            rayDataProvider?.ProvideData(ref material);
            foreach (MaterialDataProvider provider in materialDataProviders)
                provider?.ProvideData(ref material);
        }

        void OnDrawGizmos()
        {
            if (!drawGizmos || !IsInitialized) return;

            nodeBufferProvider.DrawGizmos();
        }
#endif

        [ContextMenu("Setup Material")]
        public void SetupMaterial()
        {
            if (shapeProviders == null || shapeProviders.Count == 0) return;

            if (IsInitialized)
                CleanupMaterial();
            
            material = new Material(shader);
            rayDataProvider?.ProvideData(ref material);
            foreach (MaterialDataProvider provider in materialDataProviders)
                provider?.ProvideData(ref material);

            int providerCount = shapeProviders.Count;
            leafBounds = new NativeArray<Aabb>(providerCount, Allocator.Persistent);
            shapeData = new ShapeData[providerCount];
            colorData = new ColorData[providerCount];

            UpdateBufferData();

            nodeBufferProvider = new BvhBufferProvider();
            nodeBufferProvider.InitializeBuffer(ref material, leafBounds);

            shapeBufferProvider = new ShapeBufferProvider();
            shapeBufferProvider.InitializeBuffer(ref material, shapeData);

            colorBufferProvider = new ColorBufferProvider();
            colorBufferProvider.InitializeBuffer(ref material, colorData);

            mainRenderer.material = material;
        }

        [ContextMenu("Cleanup Material")]
        public void CleanupMaterial()
        {
            nodeBufferProvider?.ReleaseBuffer();
            shapeBufferProvider?.ReleaseBuffer();
            colorBufferProvider?.ReleaseBuffer();

            if (leafBounds.IsCreated) leafBounds.Dispose();
            shapeData = null;
            colorData = null;

            if (Application.isEditor)
                DestroyImmediate(material);
            else
                Destroy(material);

            if (mainRenderer)
                mainRenderer.materials = Array.Empty<Material>();
        }

        public void AddShape(ShapeProvider shape)
        {
            shapeProviders.Add(shape);
        }

#if UNITY_EDITOR
        [ContextMenu("Find Shape Providers")]
        public void FindShapeProviders()
        {
            shapeProviders = new List<ShapeProvider>(GetComponentsInChildren<ShapeProvider>());
            EditorUtility.SetDirty(this);
        }
#endif

        private void UpdateBufferData()
        {
            for (int i = 0; i < shapeProviders.Count; i++)
            {
                ShapeProvider provider = shapeProviders[i];
                if (provider == null) continue;

                leafBounds[i] = provider.GetBounds();
                shapeData[i] = new ShapeData(provider);
                colorData[i] = new ColorData(provider);
            }
        }
    }
}
