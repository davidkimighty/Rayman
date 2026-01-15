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
        private NativeArray<int> primitiveIds;

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

            int boundsCount = segmentArray.Length;
            leafBounds = new NativeArray<Aabb>(boundsCount, Allocator.Persistent);
            primitiveIds = new NativeArray<int>(boundsCount, Allocator.Persistent);

            UpdateNodeBufferData();
            SetupPrimitiveIds();

            nodeBufferProvider = new BvhBufferProvider();
            nodeBufferProvider.InitializeBuffer(ref material, leafBounds, primitiveIds);

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
            if (primitiveIds.IsCreated) primitiveIds.Dispose();

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
                var knots = spline.Knots;
                int knotCount = knots.Count;

                if (knotCount > 0)
                {
                    KnotProvider prevKnot = knots[0];
                    prevKnot.SplineIndex = i;
                    prevKnot.PreviousKnot = prevKnot;

                    for (int j = 1; j < knotCount; j++)
                    {
                        KnotProvider currentKnot = knots[j];
                        currentKnot.SplineIndex = i;

                        prevKnot.NextKnot = currentKnot;
                        currentKnot.PreviousKnot = prevKnot;

                        segmentList.Add(new Segment(prevKnot, currentKnot, spline.ExtendedBounds));
                        prevKnot = currentKnot;
                    }
                    prevKnot.NextKnot = prevKnot;
                }

                knotList.AddRange(knots);
                knotIndex += knotCount;
            }
            splineArray = splines.ToArray();
            segmentArray = segmentList.ToArray();
            knotArray = knotList.ToArray();
        }

        private void SetupPrimitiveIds()
        {
            int index = 0;
            int id = 0;
            for (int i = 0; i < splines.Count; i++)
            {
                int knotCount = splines[i].Knots.Count - 1;
                for (int j = 0; j < knotCount; j++)
                    primitiveIds[index++] = id++;
                id++;
            }
        }

        private void UpdateNodeBufferData()
        {
            for (int i = 0; i < segmentArray.Length; i++)
                leafBounds[i] = segmentArray[i].GetBounds();
        }
    }
}