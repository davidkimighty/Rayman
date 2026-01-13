using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class SplineObject : MonoBehaviour
    {
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private RayDataProvider rayDataProvider;
        [SerializeField] private Shader shader;
        [SerializeField] private List<MaterialDataProvider> materialDataProviders = new();
        [SerializeField] private List<Spline> splines = new();
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif

        private Material material;
        private Spline[] splineArray;
        private Segment[] segmentArray;
        private KnotProvider[] knotArray;

        private BvhBufferProvider nodeBufferProvider;
        private SplineBufferProvider splineBufferProvider;
        private KnotBufferProvider knotBufferProvider;

        private NativeArray<Aabb> leafBounds;
        
        public bool IsInitialized => material && nodeBufferProvider != null;
        public Material Material => material;

        private void Start()
        {
            if (Application.isPlaying && setupOnStart)
                SetupMaterial();
        }

        private void LateUpdate()
        {
            if (!IsInitialized) return;

            UpdateNodeBufferData();

            // isDirty checks?
            nodeBufferProvider.SetData(leafBounds);
            splineBufferProvider.SetData(splineArray);
            knotBufferProvider.SetData(knotArray);
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
            if (splines == null || splines.Count == 0) return;

            if (material)
                CleanupMaterial();

            material = new Material(shader);
            rayDataProvider?.ProvideData(ref material);
            foreach (MaterialDataProvider provider in materialDataProviders)
                provider?.ProvideData(ref material);

            SetupBufferData();

            leafBounds = new NativeArray<Aabb>(segmentArray.Length, Allocator.Persistent);
            UpdateNodeBufferData();

            nodeBufferProvider = new BvhBufferProvider();
            nodeBufferProvider.InitializeBuffer(ref material, leafBounds);

            splineBufferProvider = new SplineBufferProvider();
            splineBufferProvider.InitializeBuffer(ref material, splineArray);

            knotBufferProvider = new KnotBufferProvider();
            knotBufferProvider.InitializeBuffer(ref material, knotArray);

            mainRenderer.material = material;
        }

        [ContextMenu("Cleanup Material")]
        public void CleanupMaterial()
        {
            nodeBufferProvider?.ReleaseBuffer();
            splineBufferProvider?.ReleaseBuffer();
            knotBufferProvider?.ReleaseBuffer();

            if (leafBounds.IsCreated) leafBounds.Dispose();

            segmentArray = null;
            knotArray = null;

            if (Application.isEditor)
                DestroyImmediate(material);
            else
                Destroy(material);

            if (mainRenderer)
                mainRenderer.materials = Array.Empty<Material>();
        }
        
        private void SetupBufferData()
        {
            List<Segment> segmentList = new();
            List<KnotProvider> knotList = new();
            int knotIndex = 0;
            
            for (int i = 0; i < splines.Count; i++)
            {
                Spline spline = splines[i];
                spline.KnotStartIndex = knotIndex;
                int knotCount = spline.Knots.Count;
                
                for (int j = 0; j < knotCount; j++)
                {
                    KnotProvider knot = spline[j];
                    knot.SplineIndex = i;
                    knot.PreviousKnot = j > 0 ? spline[j - 1] : knot;
                    knot.NextKnot = j < knotCount - 1 ? spline[j + 1] : knot;
                }
                knotIndex += knotCount;

                segmentList.AddRange(spline.GetSegments());
                knotList.AddRange(spline.Knots);
            }

            splineArray = splines.ToArray();
            segmentArray = segmentList.ToArray();
            knotArray = knotList.ToArray();
        }

        private void UpdateNodeBufferData()
        {
            for (int i = 0; i < segmentArray.Length; i++)
            {
                Segment segment = segmentArray[i];
                if (segment == null) continue;

                leafBounds[i] = segment.GetBounds();
            }
        }
    }
}