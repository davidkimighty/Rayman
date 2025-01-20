using UnityEngine;

namespace Rayman
{
    public class RaymarchDebugger : DebugElement
    {
        [SerializeField] private RaymarchDataProvider provider;
        
        public override string GetDebugMessage()
        {
            return provider.GetDebugMessage();
        }
    }
}
