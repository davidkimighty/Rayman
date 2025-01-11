using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public abstract class SpatialStructure : MonoBehaviour, INodeDataProvider
    {
        public abstract void Setup(List<RaymarchEntity> entities);

        public abstract void SetData(GraphicsBuffer nodeBuffer);
    }
}
