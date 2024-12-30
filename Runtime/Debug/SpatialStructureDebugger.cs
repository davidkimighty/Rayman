using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class SpatialStructureDebugger : MonoBehaviour, IDebug
    {
        private static List<ISpatialStructure<AABB>> SpatialStructures = new();

        public static void Add(ISpatialStructure<AABB> structure)
        {
            if (SpatialStructures.Contains(structure)) return;

            SpatialStructures.Add(structure);
        }

        public static void Remove(ISpatialStructure<AABB> structure)
        {
            if (!SpatialStructures.Contains(structure)) return;

            int i = SpatialStructures.IndexOf(structure);
            SpatialStructures.RemoveAt(i);
        }
        
        public string GetDebugMessage()
        {
            int sum = SpatialStructures.Sum(s => s?.Count ?? 0);
            int maxHeight = SpatialStructures.Max(s => s?.MaxHeight ?? 0);
            return $"BVH {SpatialStructures.Count} [ Nodes {sum,4}, Max Height {maxHeight,2} ]";
        }
    }
}
