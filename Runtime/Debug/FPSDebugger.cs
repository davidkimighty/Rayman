using System.Linq;
using UnityEngine;

namespace Rayman
{
    public class FPSDebugger : DebugElement
    {
        private int _frameIndex;
        private float[] _frameDeltaTimes;
        private int _frameValue;
        
        private int _msIndex;
        private float[] _frameMilliseconds;
        private float _msValue;
        
        private void Awake()
        {
            _frameDeltaTimes = new float[50];
            _frameMilliseconds = new float[30];
        }

        private void Update()
        {
            _frameDeltaTimes[_frameIndex] = Time.unscaledDeltaTime;
            _frameIndex = (_frameIndex + 1) % _frameDeltaTimes.Length;
            
            _frameMilliseconds[_msIndex] = Time.unscaledDeltaTime;
            _msIndex = (_msIndex + 1) % _frameMilliseconds.Length;

            _frameValue = Mathf.RoundToInt(GetAverageFPS());
            _msValue = GetAverageMS();
            //_msValue = Time.unscaledDeltaTime * 1000f;
        }
        
        public override string GetDebugMessage()
        {
            return $"FPS {_frameValue,3} [ {_msValue:00.00} ms ]";
        }

        private float GetAverageFPS()
        {
            float fpsTotal = _frameDeltaTimes.Sum();
            return _frameDeltaTimes.Length / fpsTotal;
        }
        
        private float GetAverageMS()
        {
            float msTotal = _frameMilliseconds.Sum();
            float avg = msTotal / _frameMilliseconds.Length;
            return avg * 1000f;
        }
    }
}
