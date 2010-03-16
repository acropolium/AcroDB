namespace AcroDB.InMemory
{
    public class DataContextPublisher : IDataContextPublisher
    {
        public void FillDataContextDescription(DataContextDescription description)
        {
            description.Name = "InMemory";
            description.ConnectionProviderType = null;
            description.DataContext = typeof(InMemoryDataContext);
            description.DataProvider = typeof(InMemoryDataProvider<,>);
        }
    }
}