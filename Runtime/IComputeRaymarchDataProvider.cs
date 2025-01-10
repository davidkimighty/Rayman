namespace Rayman
{
    public interface IComputeRaymarchDataProvider
    {
        ShapeData[] GetShapeData();
        AabbNodeData[] GetNodeData();
    }
}

