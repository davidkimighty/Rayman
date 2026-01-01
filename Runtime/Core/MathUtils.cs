using UnityEngine;

namespace Rayman
{
    public static class MathUtils
    {
        public static Vector3 Clamp(this Vector3 v, float min, float max)
        {
            return new Vector3(Mathf.Clamp(v.x, min, max), Mathf.Clamp(v.y, min, max), Mathf.Clamp(v.z, min, max));
        }
        
        public static Vector3 Clamp01(this Vector3 v)
        {
            return Vector3.Max(Vector3.Min(v, Vector3.one), Vector3.zero);
        }

        public static Vector3 Mul(this Vector3 a, Vector3 b)
        {
            return Vector3.Scale(a, b);
        }
        
        public static Vector3 Div(this Vector3 v, Vector3 d)
        {
            return new Vector3(v.x / d.x, v.y / d.y, v.z / d.z);
        }

        public static Vector3 Sqrt(this Vector3 v)
        {
            return new Vector3(Mathf.Sqrt(v.x), Mathf.Sqrt(v.y), Mathf.Sqrt(v.z));
        }
    }
}