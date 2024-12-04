using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Rayman.Debug
{
    public interface IDebug
    {
        string GetDebugMessage();
    }
    
    public class DebugDashboard : MonoBehaviour
    {
        [SerializeField] private List<Component> _debugProviders;
        [SerializeField] private TMP_Text _debugMessageText;
        [SerializeField] private float _interval = 0.1f;

        private IDebug[] _debugs;
        private float _elapsedTime = Mathf.Infinity;
        
        private void Awake()
        {
            _debugs = GetComponentsInChildren<IDebug>();
        }

        private void LateUpdate()
        {
            if (_interval > 0)
            {
                _elapsedTime += Time.deltaTime;
                if (_elapsedTime < _interval) return;
                _elapsedTime = 0;
            }
            
            StringBuilder builder = new();
            for (int i = 0; i < _debugs.Length; i++)
            {
                IDebug debug = _debugs[i];
                builder.Append($"{debug.GetDebugMessage()}      ");
            }
            _debugMessageText.text = builder.ToString();
        }
    }
}
