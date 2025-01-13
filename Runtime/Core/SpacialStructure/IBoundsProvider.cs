namespace Rayman
{
    public interface IBoundsProvider
    {
        public float AdditionalExpandBounds { get; }
        public float UpdateBoundsThreshold { get; }
        
        T GetBounds<T>() where T : struct, IBounds<T>;
    }
}
