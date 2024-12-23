namespace Rayman
{
    public interface IComputeRaymarchDataProvider
    {
        ShapeData[] GetShapeData();
        DistortionData[] GetDistortionData();
        NodeData<AABB>[] GetNodeData();
    }
}

