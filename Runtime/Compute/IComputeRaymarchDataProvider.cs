namespace Rayman
{
    public interface IComputeRaymarchDataProvider
    {
        ShapeData[] GetShapeData();
        NodeDataAabb[] GetNodeData();
    }
}

