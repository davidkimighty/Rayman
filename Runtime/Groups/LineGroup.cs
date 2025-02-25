using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    [ExecuteInEditMode]
    public class LineGroup : RaymarchGroup
    {
        [SerializeField] private List<RaymarchLineEntity> lines = new();
        [SerializeField] private List<RaymarchDataProvider> dataProviders = new();
        [SerializeField] private ColorUsages ColorUsage = ColorUsages.Color;
        [SerializeField] private float updateBoundsThreshold;
#if UNITY_EDITOR
        [SerializeField] private bool drawGizmos;
#endif
        
        private RaymarchLineEntity[] activeLines;
        private IBufferProvider nodeBufferProvider;
        private IBufferProvider lineBufferProvider;
        private IBufferProvider pointBufferProvider;
        private GraphicsBuffer nodeBuffer;
        private GraphicsBuffer lineBuffer;
        private GraphicsBuffer pointBuffer;
        
        private void LateUpdate()
        {
            if (!IsInitialized()) return;

            nodeBufferProvider.SetData(ref nodeBuffer);
            lineBufferProvider.SetData(ref lineBuffer);
            pointBufferProvider.SetData(ref pointBuffer);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsInitialized()) return;
        
            foreach (RaymarchDataProvider provider in dataProviders)
                provider?.SetupShaderProperties(ref MatInstance);
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || !IsInitialized()) return;
            
            nodeBufferProvider.DrawGizmos();
        }
#endif
        
        public override Material InitializeGroup()
        {
            activeLines = lines.Where(s => s && s.gameObject.activeInHierarchy).ToArray();
            if (activeLines.Length == 0) return null;

            MatInstance = new Material(shader);
            if (!MatInstance) return null;
            
            foreach (RaymarchDataProvider provider in dataProviders)
                provider?.SetupShaderProperties(ref MatInstance);

            nodeBufferProvider = new BvhAabbNodeBufferProvider(updateBoundsThreshold);
            nodeBuffer = nodeBufferProvider.InitializeBuffer(activeLines, ref MatInstance);
            
            lineBufferProvider = new LineBufferProvider<LineData>();
            lineBuffer = lineBufferProvider.InitializeBuffer(activeLines, ref MatInstance);
            
            pointBufferProvider = new PointBufferProvider<PointData>();
            pointBuffer = pointBufferProvider.InitializeBuffer(activeLines, ref MatInstance);
            
            InvokeOnSetup();
            return MatInstance;
        }

        public override void ReleaseGroup()
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
            lines = RaymarchUtils.GetChildrenByHierarchical<RaymarchLineEntity>(transform);
        }
#endif
    }
}
