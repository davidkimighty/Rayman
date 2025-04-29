using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayman
{
    public enum Lines
    {
        Segment = 2,
        QuadraticBezier,
        CubicBezier,
    }
    
    [ExecuteInEditMode]
    public class LineObject : RaymarchObject
    {
        public class Segment : IBoundsProvider
        {
            public static Dictionary<Type, Delegate> BoundsGenerator = new();
        
            public Transform[] Points;
            public float RadiusA;
            public float RadiusB;

            static Segment()
            {
                BoundsGenerator.Add(typeof(Aabb), (Func<Transform[], Aabb>)CreateAabb);
            }

            public Segment(Transform[] points, float radiusA, float radiusB)
            {
                Points = points;
                RadiusA = radiusA;
                RadiusB = radiusB;
            }
            
            public T GetBounds<T>() where T : struct, IBounds<T>
            {
                if (!BoundsGenerator.TryGetValue(typeof(T), out Delegate creator))
                    throw new InvalidOperationException($"Unsupported bounds type: {typeof(T)}");
            
                var boundsCreator = creator as Func<Transform[], T>;
                T bounds = boundsCreator(Points);
                return bounds.Expand(Mathf.Max(RadiusA, RadiusB));
            }

            private static Aabb CreateAabb(Transform[] points)
            {
                Vector3 origin = points[0].position;
                Aabb aabb = new(origin, origin);
                for (int i = 1; i < points.Length; i++)
                    aabb = aabb.Include(points[i].position);
                return aabb;
            }
        }
        
        public const string GradientColorKeyword = "GRADIENT_COLOR";

        [SerializeField] private Lines line = Lines.Segment;
        [SerializeField] private List<Transform> points = new();
        [SerializeField] private Vector2 radius = Vector2.one * 0.1f;
        [SerializeField] private bool useLossyScale = true;
        [SerializeField] private float syncThreshold;
        [SerializeField] private Color color = Color.white;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif
        
        private Segment[] activeSegments;
        private INodeBufferProvider nodeBufferProvider;
        private IBufferProvider<Segment> segmentBufferProvider;
        private IBufferProvider<Segment> pointBufferProvider;
        private GraphicsBuffer nodeBuffer;
        private GraphicsBuffer segmentBuffer;
        private GraphicsBuffer pointBuffer;
        
        private void LateUpdate()
        {
            if (!IsReady()) return;

            nodeBufferProvider.SyncBounds(activeSegments, syncThreshold);
            nodeBufferProvider.SetData(ref nodeBuffer);
            segmentBufferProvider.SetData(ref segmentBuffer);
            pointBufferProvider.SetData(ref pointBuffer);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsReady()) return;
        
            ProvideShaderProperties();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || !IsReady()) return;
            
            nodeBufferProvider.DrawGizmos();
        }
#endif
        
        public override Material SetupMaterial()
        {
            activeSegments = GetActiveSegments();
            if (activeSegments.Length == 0) return null;

            MatInstance = new Material(shader);
            if (!MatInstance) return null;
            
            ProvideShaderProperties();
            
            nodeBufferProvider = new BvhNodeBufferProvider<Aabb, AabbNodeData>();
            nodeBuffer = nodeBufferProvider.InitializeBuffer(activeSegments, ref MatInstance);
            
            segmentBufferProvider = new SegmentBufferProvider<SegmentData>();
            segmentBuffer = segmentBufferProvider.InitializeBuffer(activeSegments, ref MatInstance);
            
            pointBufferProvider = new PointBufferProvider<PointData>();
            pointBuffer = pointBufferProvider.InitializeBuffer(activeSegments, ref MatInstance);
            
            InvokeOnSetup();
            return MatInstance;
        }

        public override void Cleanup()
        {
            if (Application.isEditor)
                DestroyImmediate(MatInstance);
            else
                Destroy(MatInstance);
            activeSegments = null;
            
            nodeBufferProvider?.ReleaseData();
            nodeBufferProvider = null;
            segmentBufferProvider?.ReleaseData();
            segmentBufferProvider = null;
            pointBufferProvider?.ReleaseData();
            pointBufferProvider = null;
            
            nodeBuffer?.Release();
            segmentBuffer?.Release();
            pointBuffer?.Release();
            InvokeOnCleanup();
        }
        
        public override bool IsReady() => MatInstance &&
            nodeBufferProvider != null && segmentBufferProvider != null;

        protected override void ProvideShaderProperties()
        {
            base.ProvideShaderProperties();
            MatInstance.SetInt("_LineType", (int)line);
            MatInstance.SetColor("_Color", color);
        }

        private Segment[] GetActiveSegments()
        {
            Transform[] activePoints = points.Where(x => x && x.gameObject.activeInHierarchy).ToArray();
            int pointCount = activePoints.Length;
            int pointCountPerLine = (int)line;
            int maxLines = (pointCount - 1) / (pointCountPerLine - 1);
            
            List<Segment> segments = new();
            if (maxLines <= 0) return segments.ToArray();

            int pointIndex = 0;
            for (int i = 0; i < maxLines; i++)
            {
                Transform[] segment = new Transform[pointCountPerLine];
                for (int j = 0; j < pointCountPerLine; j++)
                    segment[j] = activePoints[pointIndex + j];

                float t = (float)i / Mathf.Max(1, maxLines - 1);
                float a = Mathf.Lerp(radius.x, radius.y, t);
                float b = Mathf.Lerp(radius.x, radius.y, Mathf.Clamp01(t + 1f / Mathf.Max(1, maxLines - 1)));
                segments.Add(new Segment(segment, a, b));
                pointIndex += pointCountPerLine - 1;
            }
            return segments.ToArray();
        }
        
#if UNITY_EDITOR
        [ContextMenu("Find All Points")]
        public void FindAllPoints()
        {
            points = RaymarchUtils.GetChildrenByHierarchical<Transform>(transform);
        }
#endif
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SegmentData : ISetupFromIndexed<LineObject.Segment>
    {
        public Vector2 Radius;
        public int StartIndex;

        public int Index
        {
            get => StartIndex;
            set => StartIndex = value;
        }

        public void SetupFrom(LineObject.Segment data, int index)
        {
            Radius = new Vector2(data.RadiusA, data.RadiusB);
            StartIndex = index;
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct PointData : ISetupFrom<Transform>
    {
        public Vector3 Position;

        public void SetupFrom(Transform data)
        {
            Position = data.position;
        }
    }
}
