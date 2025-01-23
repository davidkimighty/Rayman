namespace Rayman
{
    public class ShapeColorBufferProvider : ShapeBufferProvider<RaymarchShapeColor, ShapeColorData>, IRaymarchDebugProvider
    {
        protected override int GetStride() => ShapeColorData.Stride;

        protected override ShapeColorData CreateData(RaymarchShapeColor shape)
        {
            return new ShapeColorData(shape);
        }

        public int GetDebugInfo()
        {
            return shapes.Length;
        }
    }
}