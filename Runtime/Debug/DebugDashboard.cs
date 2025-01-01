using TMPro;
using UnityEngine;

namespace Rayman
{
    public class DebugDashboard : MonoBehaviour
    {
        private const string Space = "      ";
        
        [SerializeField] private TMP_Text _debugMessageText;
        [SerializeField] private Transform _textHolder;
        [SerializeField] private float _interval = 0.05f;
        
        private IDebug[] _debugProviders;
        private TMP_Text[] _activeTexts;
        private float _elapsedTime = Mathf.Infinity;

        private void Start()
        {
            _debugProviders = GetComponents<IDebug>();
            _activeTexts = new TMP_Text[_debugProviders.Length];

            _activeTexts[0] = _debugMessageText;
            for (int i = 1; i < _debugProviders.Length; i++)
                _activeTexts[i] = Instantiate(_debugMessageText, _textHolder);
        }

        private void Update()
        {
            if (_interval > 0)
            {
                _elapsedTime += Time.deltaTime;
                if (_elapsedTime < _interval) return;
                _elapsedTime = 0;
            }
            
            for (int i = 0; i < _debugProviders.Length; i++)
                _activeTexts[i].text = _debugProviders[i].GetDebugMessage() + Space;
        }
    }
}
