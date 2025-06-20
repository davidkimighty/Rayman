using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class SdfDebugger : DebugElement
    {
        [SerializeField] private RaymarchManager raymarchManager;
        private int totalSdfCount;

        private void Start()
        {
            if (raymarchManager == null)
                raymarchManager = FindFirstObjectByType<RaymarchManager>();
            if (raymarchManager == null) return;
            
            if (raymarchManager != null)
            {
                totalSdfCount = raymarchManager.Renderers.Sum(r => r.RaymarchObjects.OfType<IRaymarchDebug>()
                    .Sum(g => g.GetSdfCount()));
                raymarchManager.OnAddRenderer += (r) => totalSdfCount += r.RaymarchObjects.OfType<ISpatialStructureDebug>()
                    .Sum(g => g.GetNodeCount());
                raymarchManager.OnRemoveRenderer += (r) => totalSdfCount -= r.RaymarchObjects.OfType<ISpatialStructureDebug>()
                    .Sum(g => g.GetMaxHeight());
                return;
            }

            totalSdfCount = FindObjectsByType<RaymarchRenderer>(FindObjectsSortMode.None)
                .Sum(r => r.RaymarchObjects.OfType<IRaymarchDebug>().Sum(g => g.GetSdfCount()));
        }

        public override string GetDebugMessage()
        {
            return $"SDF {totalSdfCount,4}";
        }
    }
    
    public interface IRaymarchDebug
    {
        int GetSdfCount();
    }
}
