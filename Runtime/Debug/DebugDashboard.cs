using TMPro;
using UnityEngine;

namespace Rayman
{
    public class DebugDashboard : MonoBehaviour
    {
        private const string Space = "      ";
        
        [SerializeField] private TMP_Text _debugMessageText;

        private IDebug[] _debugProviders;

        private void Start()
        {
            _debugProviders = GetComponents<IDebug>();
        }

        private void Update()
        {
            string message = string.Empty;
            for (int i = 0; i < _debugProviders.Length; i++)
            {
                message += _debugProviders[i].GetDebugMessage() + Space;
                _debugMessageText.text = message;
            }
        }
    }
}
