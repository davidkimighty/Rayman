using UnityEngine;

namespace Rayman.Debug
{
    public class SpatialStructureDebugger : MonoBehaviour, IDebug
    {
        // private ComputeRaymarchManager manager;
        //
        // private void Awake()
        // {
        //     manager = FindAnyObjectByType<ComputeRaymarchManager>();
        // }

        public string GetDebugMessage()
        {
            return $"Nodes   [ {0,4} ]    " +
                   $"Max Depth   [ {0,3} ]";
        }
    }
}
