using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class SpatialStructureDebugger : DebugElement
    {
        private ISpatialStructureDebugProvider[] providers;
        
        private void Awake()
        {
            providers = FindObjectsByType<RaymarchBufferProvider>(FindObjectsSortMode.None)
                .OfType<ISpatialStructureDebugProvider>().ToArray();
        }

        public override string GetDebugMessage()
        {
            int sum = providers.Sum(p => p.GetDebugInfo().nodeCount);
            int maxHeight = providers.Max(p => p.GetDebugInfo().maxHeight);
            return $"BVH {providers.Length} [ Nodes {sum,4}, Max Height {maxHeight,2} ]";
        }
    }
    
    public interface ISpatialStructureDebugProvider
    {
        (int nodeCount, int maxHeight) GetDebugInfo();
    }
}
