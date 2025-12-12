using UnityEngine;

namespace Rayman
{
    public interface IMaterialProvider
    {
        Material Material { get; }

        Material CreateMaterial();
        void Cleanup();
    }
}
