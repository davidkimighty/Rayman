using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class SpatialStructureDebugger : MonoBehaviour, IDebug
    {
        private static List<RaymarchRenderer> RaymarchRenderers = new();
        
        public static void Add(RaymarchRenderer raymarchRenderer)
        {
            if (RaymarchRenderers.Contains(raymarchRenderer)) return;

            RaymarchRenderers.Add(raymarchRenderer);
        }

        public static void Remove(RaymarchRenderer raymarchRenderer)
        {
            if (!RaymarchRenderers.Contains(raymarchRenderer)) return;

            int i = RaymarchRenderers.IndexOf(raymarchRenderer);
            RaymarchRenderers.RemoveAt(i);
        }
        
        public string GetDebugMessage()
        {
            int sum = RaymarchRenderers.Sum(r => r.NodeCount);
            int maxHeight = RaymarchRenderers.Max(r => r.MaxHeight);
            return $"BVH {RaymarchRenderers.Sum(r => r.SpatialStructureCount)} [ Nodes {sum,4}, Max Height {maxHeight,2} ]";
        }
    }
}
