using System;
using System.Reflection;
using AcroDB;
using AcroDB.Attributes;
using AcroDB.MsSql;

namespace AcroDBTest
{
    [AcroDbEntity]
    //[AcroDbEntity(CustomEntity = typeof(UserEntityPrototype))]
    public interface IUserEntity
    {
        Guid ID { get; set; }
        string Name { get; set; }
        string Surname { get; set; }
    }

    [AcroDbEntity]
    public interface Post
    {
        Guid ID { get; set; }
        string Title { get; set; }
        string Body { get; set; }
        string BodyLong { get; set; }
    }

    [AcroDbEntity]
    public interface Users
    {
        Guid ID { get; set; }
        string Title { get; set; }
        [AcroColumnStringLength(60)]
        string Body { get; set; }
        [AcroColumnLongString]
        string BodyLong { get; set; }
    }

    class Program
    {
        static void Main()
        {
            AcroDataContext.DefaultDataProvider = typeof (MsSqlDataProvider<,>);
            AcroDataContext.DefaultDataContext = typeof(MsSqlDataContext);
            AcroDataContext.DefaultDataContextParams = new [] { @"server=OLEKSIY-DEVPC\SQLEXPRESS;Database=acrodbtest;Trusted_Connection=True;" };
            //AcroDataContext.DefaultDataProvider = typeof (MongoDbDataProvider<,>);
            //AcroDataContext.DefaultDataContext = typeof(MongoDbDataContext);
            //AcroDataContext.DefaultDataContextParams = new[] { @"acrodblibtest", @"localhost", "27017", @"" };
            AcroDataContext.ResetEntityMaps();
            AcroDataContext.ScanAssemblyForEntities(Assembly.GetExecutingAssembly());
            AcroDataContext.PerformMigrations();

            using (var manager = AcroDataContext.Go)
            {
                var usr = manager.Provide<IUserEntity>().Create();
                usr.Surname = DateTime.Now.ToLongDateString();
                usr.Name = DateTime.Now.ToLongTimeString();
                manager.Provide<IUserEntity>().Save(usr);
                manager.SubmitChanges();
                foreach (var u in manager.Provide<IUserEntity>().GetFiltered(x => x.Name.Equals("20:30:51"), x => x.ID))
                {
                    Console.WriteLine(u.Name);
                }
            }
            Console.ReadKey();
        }
    }
}
