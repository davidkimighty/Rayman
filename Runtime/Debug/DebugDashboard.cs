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

        private IDebug[] _debugs;
        
        private void Awake()
        {
            _debugs = GetComponentsInChildren<IDebug>();
        }

        private void LateUpdate()
        {
            StringBuilder builder = new();
            for (int i = 0; i < _debugs.Length; i++)
            {
                IDebug debug = _debugs[i];
                builder.Append($"{debug.GetDebugMessage()}   ");
            }
            _debugMessageText.text = builder.ToString();
        }
    }
}
