using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class SplineObject : RaymarchObject
    {
        [SerializeField] private List<Spline> splines = new();
        [SerializeField] private BufferProvider<Spline> splineBufferProvider;
        [SerializeField] private BufferProvider<KnotProvider> knotBufferProvider;
        [SerializeField] private BufferProvider<IBoundsProvider> nodeBufferProvider;

        private KnotProvider[] knots;
        private Segment[] segments;
        
        private void LateUpdate()
        {
            if (!material) return;
            
            splineBufferProvider?.SetData();
            knotBufferProvider?.SetData();
            nodeBufferProvider?.SetData();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!material) return;
            
            SetMaterialData();
        }
#endif

        public override Material CreateMaterial()
        {
            if (material)
                Cleanup();
            material = new Material(shader);
            
            SetMaterialData();
            SetupDataProviders();
            
            splineBufferProvider?.InitializeBuffer(ref material, splines.ToArray());
            knotBufferProvider?.InitializeBuffer(ref material, knots);
            nodeBufferProvider?.InitializeBuffer(ref material, segments);
            
            return material;
        }

        public override void Cleanup()
        {
            splineBufferProvider?.ReleaseBuffer();
            knotBufferProvider?.ReleaseBuffer();
            nodeBufferProvider?.ReleaseBuffer();
            
            if (Application.isEditor)
                DestroyImmediate(material);
            else
                Destroy(material);
        }

        private void SetupDataProviders()
        {
            List<KnotProvider> allKnots = new();
            List<Segment> allSegments = new();
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
            knots = allKnots.ToArray();
            segments = allSegments.ToArray();
        }
    }
}