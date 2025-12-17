namespace Rayman
{
    public interface IPopulateData<T> where T : class
    {
        void Populate(T provider);
    }
}