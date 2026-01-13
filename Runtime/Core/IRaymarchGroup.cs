namespace Rayman
{
    

    public interface IRaymarchGroup
    {
        OperationType Operation { get; }
        float Blend { get; }
        bool IsGroupDirty { get; set; }
    }
}
