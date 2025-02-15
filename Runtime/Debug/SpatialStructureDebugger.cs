using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class SpatialStructureDebugger : DebugElement
    {
        [SerializeField] private RaymarchManager raymarchManager;
        private int totalGroupCount;
        private int totalNodeCount;
        private int maxHeight;
        
        private void Start()
        {
            if (raymarchManager == null)
                raymarchManager = FindFirstObjectByType<RaymarchManager>();
            if (raymarchManager != null)
            {
                totalGroupCount = raymarchManager.Renderers.Sum(r => r.Groups.Count);
                totalNodeCount = raymarchManager.Renderers.Sum(r => r.NodeCount);
                maxHeight = raymarchManager.Renderers.Max(r => r.MaxHeight);
                
                raymarchManager.OnAddRenderer += (r) =>
                {
                    totalGroupCount += r.Groups.Count;
                    totalNodeCount += r.NodeCount;
                    maxHeight = Mathf.Max(maxHeight, r.MaxHeight);
                };
                raymarchManager.OnRemoveRenderer += (r) =>
                {
                    totalGroupCount -= r.Groups.Count;
                    totalNodeCount -= r.NodeCount;
                };
                return;
            }

            RaymarchRenderer[] renderers = FindObjectsByType<RaymarchRenderer>(FindObjectsSortMode.None);
            totalGroupCount = renderers.Sum(r => r.Groups.Count);
            totalNodeCount = renderers.Sum(r => r.NodeCount);
            maxHeight = renderers.Max(r => r.MaxHeight);
        }

        public override string GetDebugMessage()
        {
            return $"BVH {totalGroupCount} [ Nodes {totalNodeCount,4}, Max Height {maxHeight,2} ]";
        }
    }
    
    public interface ISpatialStructureDebug
    {
        int GetNodeCount();
        int GetMaxHeight();
    }
}
