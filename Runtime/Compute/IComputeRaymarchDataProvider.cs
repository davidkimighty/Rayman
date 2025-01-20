namespace Rayman
{
    public interface IComputeRaymarchDataProvider
    {
        ShapeData[] GetShapeData();
        NodeDataAABB[] GetNodeData();
    }
}

