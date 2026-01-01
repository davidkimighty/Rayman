using UnityEngine;

namespace Rayman
{
    public interface IMaterialDataProvider
    {
        void ProvideData(ref Material material);
    }
}
