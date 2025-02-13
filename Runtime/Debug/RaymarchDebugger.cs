using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class RaymarchDebugger : DebugElement
    {
        private IRaymarchDebugProvider[] providers;

        private void Awake()
        {
            // providers = FindObjectsByType<RaymarchBufferProvider>(FindObjectsSortMode.None)
            //     .OfType<IRaymarchDebugProvider>().ToArray();
        }

        public override string GetDebugMessage()
        {
            return $"SDF {providers.Sum(p => p.GetDebugInfo()),4}";
        }
    }
    
    public interface IRaymarchDebugProvider
    {
        int GetDebugInfo();
    }
}
