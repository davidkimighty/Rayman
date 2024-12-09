namespace Rayman
{
    public interface IBoundsSource
    {
        T GetBounds<T>() where T : struct, IBounds<T>;
    }
}
