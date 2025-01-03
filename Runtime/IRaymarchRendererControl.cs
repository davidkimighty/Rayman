namespace Rayman
{
    public interface IRaymarchRendererControl
    {
        void AddShape(RaymarchShape shape, int groupId);
        void RemoveShape(RaymarchShape shape, int groupId);
    }
}
