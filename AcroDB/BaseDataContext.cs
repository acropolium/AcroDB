namespace AcroDB
{
    public abstract class BaseDataContext : IDataContext
    {
        protected BaseDataContext(string[] parameters) { }

        public abstract void SubmitChanges();
        public virtual void Dispose() { }
    }
}
