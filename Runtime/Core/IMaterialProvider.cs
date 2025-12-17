using System;
using UnityEngine;

namespace Rayman
{
    public interface IMaterialProvider
    {
        event Action<IMaterialProvider> OnCreateMaterial;
        event Action<IMaterialProvider> OnCleanupMaterial;
        
        Material Material { get; }

        Material CreateMaterial();
        void Cleanup();
    }
}
