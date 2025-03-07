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
                totalGroupCount = raymarchManager.Renderers.Sum(r => r.RaymarchObjects.Count);
                totalNodeCount = raymarchManager.Renderers.Sum(r => r.RaymarchObjects.OfType<ISpatialStructureDebug>()
                    .Sum(g => g.GetNodeCount()));
                maxHeight = raymarchManager.Renderers.Max(r => r.RaymarchObjects.OfType<ISpatialStructureDebug>()
                    .Sum(g => g.GetMaxHeight()));
                
                raymarchManager.OnAddRenderer += (r) =>
                {
                    totalGroupCount += r.RaymarchObjects.Count;
                    totalNodeCount += r.RaymarchObjects.OfType<ISpatialStructureDebug>().Sum(g => g.GetNodeCount());
                    maxHeight = Mathf.Max(maxHeight, r.RaymarchObjects.OfType<ISpatialStructureDebug>().Sum(g => g.GetMaxHeight()));
                };
                raymarchManager.OnRemoveRenderer += (r) =>
                {
                    totalGroupCount -= r.RaymarchObjects.Count;
                    totalNodeCount -= r.RaymarchObjects.OfType<ISpatialStructureDebug>().Sum(g => g.GetNodeCount());
                };
                return;
            }

            RaymarchRenderer[] renderers = FindObjectsByType<RaymarchRenderer>(FindObjectsSortMode.None);
            totalGroupCount = renderers.Sum(r => r.RaymarchObjects.Count);
            totalNodeCount = renderers.Sum(r => r.RaymarchObjects.OfType<ISpatialStructureDebug>().Sum(g => g.GetNodeCount()));
            maxHeight = renderers.Max(r => r.RaymarchObjects.OfType<ISpatialStructureDebug>().Sum(g => g.GetMaxHeight()));
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
