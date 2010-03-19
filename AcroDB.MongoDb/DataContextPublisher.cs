﻿namespace AcroDB.MongoDb
{
    public class DataContextPublisher : IDataContextPublisher
    {
        public void FillDataContextDescription(IDataContextDescription description)
        {
            description.Name = "MongoDb";
            description.ConnectionProviderType = null;
            description.DataContext = typeof(MongoDbDataContext);
            description.DataProvider = typeof(MongoDbDataProvider<,>);
        }
    }
}