using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class SplineObject : MonoBehaviour
    {
        public const float DefaultTension = 1 / 3f;

        [SerializeField] private bool setupOnAwake = true;
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private RayDataProvider rayDataProvider;
        [SerializeField] private Shader shader;
        [SerializeField] private MaterialDataProvider materialDataProvider;
        [SerializeField] private List<Spline> splines = new();

        private Material material;
        private Segment[] segments;
        private KnotProvider[] knots;

        private INativeBufferProvider<Aabb> nodeBufferProvider;
        private IBufferProvider<SplineData> splineBufferProvider;
        private IBufferProvider<KnotData> knotBufferProvider;

        private NativeArray<Aabb> leafBounds;
        private SplineData[] splineData;
        private KnotData[] knotData;

        private void Awake()
        {
            if (setupOnAwake)
                SetupMaterial();
        }

        private void LateUpdate()
        {
            if (!material) return;

            UpdateNodeBufferData();
            UpdateSplineBufferData();
            UpdateKnotBufferData();

            // isDirty checks?
            nodeBufferProvider.SetData(leafBounds);
            splineBufferProvider.SetData(splineData);
            knotBufferProvider.SetData(knotData);
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

            SetupBufferData();

            leafBounds = new NativeArray<Aabb>(segments.Length, Allocator.Persistent);
            splineData = new SplineData[splines.Count];
            knotData = new KnotData[knots.Length];

            UpdateNodeBufferData();
            UpdateSplineBufferData();
            UpdateKnotBufferData();

            nodeBufferProvider = new BvhBufferProvider();
            nodeBufferProvider.InitializeBuffer(ref material, leafBounds);

            splineBufferProvider = new SplineBufferProvider();
            splineBufferProvider.InitializeBuffer(ref material, splineData);

            knotBufferProvider = new KnotBufferProvider();
            knotBufferProvider.InitializeBuffer(ref material, knotData);

            mainRenderer.material = material;
        }

        public void CleanupMaterial()
        {
            nodeBufferProvider?.ReleaseBuffer();
            splineBufferProvider?.ReleaseBuffer();
            knotBufferProvider?.ReleaseBuffer();

            if (leafBounds.IsCreated) leafBounds.Dispose();
            splineData = null;
            knotData = null;

            segments = null;
            knots = null;

            if (Application.isEditor)
                DestroyImmediate(material);
            else
                Destroy(material);

            if (mainRenderer)
                mainRenderer.materials = Array.Empty<Material>();
        }
        
        private void SetupBufferData()
        {
            List<Segment> allSegments = new();
            List<KnotProvider> allKnots = new();
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

                allSegments.AddRange(spline.GetSegments());
                allKnots.AddRange(spline.Knots);
            }

            segments = allSegments.ToArray();
            knots = allKnots.ToArray();
        }

        private void UpdateNodeBufferData()
        {
            for (int i = 0; i < segments.Length; i++)
            {
                Segment segment = segments[i];
                if (segment == null) continue;

                leafBounds[i] = segment.GetBounds();
            }
        }

        private void UpdateSplineBufferData()
        {
            for (int i = 0; i < splines.Count; i++)
                splineData[i] = new SplineData(splines[i]);
        }

        private void UpdateKnotBufferData()
        {
            for (int i = 0; i < knots.Length; i++)
            {
                KnotProvider knot = knots[i];
                if (knot.TangentMode == TangentMode.Auto)
                {
                    Vector3 prevPos = knot.PreviousKnot.transform.position;
                    Vector3 nextPos = knot.NextKnot.transform.position;
                    Vector3 currentPos = knot.transform.position;
                    Vector3 autoTangent = GetAutoSmoothTangent(prevPos, currentPos, nextPos);
                    knot.TangentOut = autoTangent;
                    knot.TangentIn = -autoTangent;
                }
                else if (knot.TangentMode == TangentMode.Linear)
                {
                    knot.TangentOut = Vector3.zero;
                    knot.TangentIn = Vector3.zero;
                }
                knotData[i] = new KnotData(knot);
            }
        }

        private float3 GetAutoSmoothTangent(float3 previous, float3 current, float3 next, float tension = DefaultTension)
        {
            var d1 = math.length(current - previous);
            var d2 = math.length(next - current);

            if (d1 == 0f)
                return (next - current) * 0.1f;
            else if (d2 == 0f)
                return (current - previous) * 0.1f;

            var a = tension;
            var twoA = 2f * tension;

            var d1PowA = math.pow(d1, a);
            var d1Pow2A = math.pow(d1, twoA);
            var d2PowA = math.pow(d2, a);
            var d2Pow2A = math.pow(d2, twoA);

            return (d1Pow2A * next - d2Pow2A * previous + (d2Pow2A - d1Pow2A) * current) / (3f * d1PowA * (d1PowA + d2PowA));
        }
    }
}