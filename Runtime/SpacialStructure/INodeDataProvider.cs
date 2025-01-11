using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public interface INodeDataProvider
    {
        void Setup(List<RaymarchEntity> entities);
        void SetData(GraphicsBuffer nodeBuffer);
    }
}
