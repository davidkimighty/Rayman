using UnityEngine;

namespace Rayman.Debug
{
    public class SpatialStructureDebugger : MonoBehaviour, IDebug
    {
        private ComputeRaymarchManager manager;
        
        private void Awake()
        {
            manager = FindAnyObjectByType<ComputeRaymarchManager>();
        }

        public string GetDebugMessage()
        {
            return $"Nodes   [ {manager.SpatialStructure?.Count ?? 0,4} ]    " +
                   $"Max Depth   [ {manager.SpatialStructure?.MaxHeight ?? 0,3} ]";
        }
    }
}