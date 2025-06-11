namespace Rayman
{
    public interface ISetupFromIndexed<T>
    {
        int Index { get; set; }
        
        void SetupFrom(T data, int index);
    }
}