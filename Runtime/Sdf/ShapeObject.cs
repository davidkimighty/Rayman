using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class ShapeObject : MonoBehaviour
    {
        [SerializeField] private bool setupOnAwake = true;
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private RayDataProvider rayDataProvider;
        [SerializeField] private Shader shader;
        [SerializeField] private MaterialDataProvider materialDataProvider;
        [SerializeField] private List<ShapeProvider> shapeProviders;

        private Material material;

        private INativeBufferProvider<Aabb> nodeBufferProvider;
        private IBufferProvider<ShapeData> shapeBufferProvider;
        private IBufferProvider<ColorData> colorBufferProvider;

        private NativeArray<Aabb> leafBounds;
        private ShapeData[] shapeData;
        private ColorData[] colorData;

        public int ShapeCount => leafBounds.Length;
        

        private void Awake()
        {
            if (setupOnAwake)
                SetupMaterial();
        }

        private void LateUpdate()
        {
            if (!material) return;

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
            if (!material)
            {
                if (!Application.isPlaying)
                    CleanupMaterial();
                return;
            }

            rayDataProvider.ProvideData(ref material);
            materialDataProvider.ProvideData(ref material);
        }
#endif

        public void SetupMaterial()
        {
            if (material)
                CleanupMaterial();
            
            material = new Material(shader);
            rayDataProvider.ProvideData(ref material);
            materialDataProvider.ProvideData(ref material);

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
