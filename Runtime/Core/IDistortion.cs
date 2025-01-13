using UnityEngine;

namespace Rayman
{
    public enum Distortions
    {
        None,
        Twist,
        Bend,
    }
    
    public interface IDistortion
    {
        Distortions Type { get; }
        Vector3 ExtraBounds { get; }
    }
}
