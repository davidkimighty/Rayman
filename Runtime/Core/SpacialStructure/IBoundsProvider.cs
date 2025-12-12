namespace Rayman
{
    public interface IBoundsProvider
    {
        T GetBounds<T>() where T : struct, IBounds<T>;
    }
}
