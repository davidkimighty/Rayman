using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class RaymarchDebugger : MonoBehaviour, IDebug
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
            int count = RaymarchRenderers.Sum(r => r.BoundingVolumes?.Length ?? 0);
            return $"SDF {count,4}";
        }
    }
}
