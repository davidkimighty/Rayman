using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class LineObject : RaymarchObject
    {
        public const string GradientColorKeyword = "GRADIENT_COLOR";
        
        [SerializeField] private List<LineElement> lines = new();
        [SerializeField] private ColorUsages ColorUsage = ColorUsages.Color;
        [SerializeField] private float syncThreshold;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif
        
        private LineElement[] activeLines;
        private INodeBufferProvider<Aabb> nodeBufferProvider;
        private IRaymarchElementBufferProvider lineBufferProvider;
        private IRaymarchElementBufferProvider pointBufferProvider;
        private GraphicsBuffer nodeBuffer;
        private GraphicsBuffer lineBuffer;
        private GraphicsBuffer pointBuffer;
        
        private void LateUpdate()
        {
            if (!IsInitialized()) return;

            for (int i = 0; i < activeLines.Length; i++)
                nodeBufferProvider.SyncBounds(i, activeLines[i].GetBounds<Aabb>(), syncThreshold);
            
            nodeBufferProvider.SetData(ref nodeBuffer);
            lineBufferProvider.SetData(ref lineBuffer);
            pointBufferProvider.SetData(ref pointBuffer);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsInitialized()) return;
        
            ProvideShaderProperties();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || !IsInitialized()) return;
            
            nodeBufferProvider.DrawGizmos();
        }
#endif
        
        public override Material Initialize()
        {
            activeLines = lines.Where(s => s && s.gameObject.activeInHierarchy).ToArray();
            if (activeLines.Length == 0) return null;

            MatInstance = new Material(shader);
            if (!MatInstance) return null;
            
            ProvideShaderProperties();

            nodeBufferProvider = new BvhAabbNodeBufferProvider();
            nodeBuffer = nodeBufferProvider.InitializeBuffer(
                activeLines.Select(s => s.GetBounds<Aabb>()).ToArray(), ref MatInstance);
            
            if (ColorUsage == ColorUsages.Gradient)
                MatInstance.EnableKeyword(GradientColorKeyword);
            lineBufferProvider = ColorUsage switch
            {
                ColorUsages.Color => new LineBufferProvider<LineData>(),
                ColorUsages.Gradient => new LineBufferProvider<GradientLineData>(),
                _ => new LineBufferProvider<LineData>()
            };
            lineBuffer = lineBufferProvider.InitializeBuffer(activeLines, ref MatInstance);
            
            pointBufferProvider = new PointBufferProvider<PointData>();
            pointBuffer = pointBufferProvider.InitializeBuffer(activeLines, ref MatInstance);
            
            InvokeOnSetup();
            return MatInstance;
        }

        public override void Release()
        {
            if (Application.isEditor)
                DestroyImmediate(MatInstance);
            else
                Destroy(MatInstance);
            activeLines = null;
            
            nodeBufferProvider?.ReleaseData();
            nodeBufferProvider = null;
            lineBufferProvider?.ReleaseData();
            lineBufferProvider = null;
            pointBufferProvider?.ReleaseData();
            pointBufferProvider = null;
            
            nodeBuffer?.Release();
            lineBuffer?.Release();
            pointBuffer?.Release();
            InvokeOnRelease();
        }
        
        public override bool IsInitialized() => MatInstance &&
            nodeBufferProvider != null && lineBufferProvider != null;
        
#if UNITY_EDITOR
        [ContextMenu("Find All Lines")]
        public void FindAllLines()
        {
            lines = RaymarchUtils.GetChildrenByHierarchical<LineElement>(transform);
        }
#endif
    }
}
