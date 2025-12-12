using UnityEngine;

namespace Rayman
{
    public abstract class VisualProvider : MonoBehaviour
    {
        public bool IsVisualDirty { get; set; } = true;
    }
}
