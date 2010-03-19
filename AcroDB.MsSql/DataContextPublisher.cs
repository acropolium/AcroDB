namespace AcroDB.MsSql
{
    public class DataContextPublisher : IDataContextPublisher
    {
        public void FillDataContextDescription(IDataContextDescription description)
        {
            description.Name = "MsSql";
            description.ConnectionProviderType = @"System.Data.SqlClient";
            description.DataContext = typeof(MsSqlDataContext);
            description.DataProvider = typeof(MsSqlDataProvider<,>);
        }
    }
}
