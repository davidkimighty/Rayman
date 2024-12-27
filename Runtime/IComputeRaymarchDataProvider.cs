namespace Rayman
{
    public interface IComputeRaymarchDataProvider
    {
        ShapeData[] GetShapeData();
        NodeData<AABB>[] GetNodeData();
    }
}

