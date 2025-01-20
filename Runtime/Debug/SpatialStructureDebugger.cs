using UnityEngine;

namespace Rayman
{
    public class SpatialStructureDebugger : DebugElement
    {
        [SerializeField] private RaymarchDataProvider provider;
        
        public override string GetDebugMessage()
        {
            return provider.GetDebugMessage();
        }
    }
}
