namespace AcroDB
{
    public interface IDataContext
    {
        void SubmitChanges();
        void Dispose();
    }
}
