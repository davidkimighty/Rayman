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
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private RayDataProvider rayDataProvider;
        [SerializeField] private Shader shader;
        [SerializeField] private List<MaterialDataProvider> materialDataProviders = new();
        [SerializeField] private List<ShapeProvider> shapeProviders;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif

        private Material material;
        private ShapeProvider[] shapes;

        private BvhBufferProvider nodeBufferProvider;
        private ShapeBufferProvider<ShapeData> shapeBufferProvider;
        private ColorBufferProvider<ColorData> colorBufferProvider;

        private NativeArray<Aabb> leafBounds;
        
        public bool IsInitialized => material && nodeBufferProvider != null;
        public Material Material => material;
        public int ShapeCount => shapeProviders.Count;
        public int NodeCount => IsInitialized ? nodeBufferProvider.DataLength : 0;

        private void Start()
        {
            if (Application.isPlaying && setupOnStart)
                SetupMaterial();
        }

        private void LateUpdate()
        {
            if (!IsInitialized) return;

            UpdateBufferData();

            // isDirty checks?
            nodeBufferProvider.SetData(leafBounds);
            shapeBufferProvider.SetData(shapes);
            colorBufferProvider.SetData(shapes);
        }

        private void OnDestroy()
        {
            CleanupMaterial();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsInitialized) return;

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
            // material.EnableKeyword("_GRADIENT_COLOR");

            shapes = shapeProviders.ToArray();

            leafBounds = new NativeArray<Aabb>(shapes.Length, Allocator.Persistent);
            UpdateBufferData();

            nodeBufferProvider = new BvhBufferProvider();
            nodeBufferProvider.InitializeBuffer(ref material, leafBounds);

            shapeBufferProvider = new ShapeBufferProvider<ShapeData>();
            shapeBufferProvider.InitializeBuffer(ref material, shapes);

            colorBufferProvider = new ColorBufferProvider<ColorData>();
            colorBufferProvider.InitializeBuffer(ref material, shapes);

            mainRenderer.material = material;
        }

        [ContextMenu("Cleanup Material")]
        public void CleanupMaterial()
        {
            nodeBufferProvider?.ReleaseBuffer();
            shapeBufferProvider?.ReleaseBuffer();
            colorBufferProvider?.ReleaseBuffer();

            if (leafBounds.IsCreated) leafBounds.Dispose();

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
            for (int i = 0; i < shapes.Length; i++)
            {
                ShapeProvider provider = shapes[i];
                if (!provider) continue;

                leafBounds[i] = provider.GetBounds();
            }
        }
    }
}
