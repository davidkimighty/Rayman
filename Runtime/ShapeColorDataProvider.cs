using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Provider/Shape Color Data")]
    public class ShapeColorDataProvider : ShapeDataProvider<RaymarchShapeColor, ShapeColorData>
    {
        protected override int GetStride() => ShapeColorData.Stride;

        protected override ShapeColorData CreateData(RaymarchShapeColor shape)
        {
            return new ShapeColorData(shape);
        }
    }
}