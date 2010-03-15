namespace AcroDB
{
    public abstract class BaseDataContext : IDataContext
    {
        protected BaseDataContext(object[] parameters) { }

        public abstract void SubmitChanges();
        public virtual void Dispose() { }
    }
}
