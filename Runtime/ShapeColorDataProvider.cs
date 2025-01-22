using System.Linq;
using UnityEngine;

namespace Rayman
{
    [CreateAssetMenu(menuName = "Rayman/Data Provider/Shape Color Data")]
    public class ShapeColorDataProvider : ShapeDataProvider<RaymarchShapeColor, ShapeColorData>
    {
        public override string GetDebugMessage()
        {
            return $"SDF {shapeDataByGroup.Sum(g => g.Value.Data.Length),4}";
        }

        protected override int GetStride() => ShapeColorData.Stride;

        protected override ShapeColorData CreateData(RaymarchShapeColor shape)
        {
            if (shape == null)
                Debug.Log("hi");
            return new ShapeColorData(shape);
        }
    }
}