using System;
using System.Reflection;
using AcroDB;
using AcroDB.Attributes;
using AcroDB.MongoDb;
using AcroDB.MsSql;

namespace AcroDBTest
{
    [AcroDbEntity]
    //[AcroDbEntity(CustomEntity = typeof(UserEntityPrototype))]
    public interface IUserEntity
    {
        [AcroColumnIsPrimaryKey]
        Guid ID { get; set; }
        string Name { get; set; }
        string Surname { get; set; }
    }

    class Program
    {
        static void Main()
        {
            //AcroDataContext.DefaultDataProvider = typeof (MsSqlDataProvider<,>);
            //AcroDataContext.DefaultDataContext = typeof(MsSqlDataContext);
            //AcroDataContext.DefaultDataContextParams = new object[] { @"server=OLEKSIY-DEVPC\SQLEXPRESS;Database=acrodbtest;Trusted_Connection=True;" };
            AcroDataContext.DefaultDataProvider = typeof (MongoDbDataProvider<,>);
            AcroDataContext.DefaultDataContext = typeof(MongoDbDataContext);
            AcroDataContext.DefaultDataContextParams = new object[] { @"acrodblibtest", @"localhost", "27017", @"" };
            AcroDataContext.ResetEntityMaps();
            AcroDataContext.ScanAssemblyForEntities(Assembly.GetExecutingAssembly());

            using (var manager = AcroDataContext.Go)
            {
                var usr = manager.Provide<IUserEntity>().Create();
                usr.Surname = DateTime.Now.ToLongDateString();
                usr.Name = DateTime.Now.ToLongTimeString();
                manager.Provide<IUserEntity>().Save(usr);
                manager.SubmitChanges();
                foreach (var u in manager.Provide<IUserEntity>().GetFiltered(x => x.Name.Contains("03"), x => x.ID))
                {
                    Console.WriteLine(u.Name);
                }
            }
            Console.ReadKey();
        }
    }
}
