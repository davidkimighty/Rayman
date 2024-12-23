using UnityEngine;

namespace Rayman
{
    public class RaymarchDebugger : MonoBehaviour, IDebug
    {
        private RaymarchShape[] _shapes;
        
        private void Awake()
        {
            _shapes = FindObjectsByType<RaymarchShape>(FindObjectsSortMode.None);
        }

        public string GetDebugMessage()
        {
            int count = _shapes?.Length ?? 0;
            return $"SDF   [ {count,4} ]";
        }
    }
}
