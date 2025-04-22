namespace Rayman
{
    public interface IBoundsProvider
    {
        T GetBounds<T>() where T : struct, IBounds<T>;
    }

    public static class BoundsUtils
    {
        public static T[] GetBounds<T>(this IBoundsProvider[] providers) where T : struct, IBounds<T>
        {
            T[] bounds = new T[providers.Length];
            for (int i = 0; i < providers.Length; i++)
                bounds[i] = providers[i].GetBounds<T>();
            return bounds;
        }
    }
}
