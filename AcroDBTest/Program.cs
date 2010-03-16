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
        Guid ID { get; set; }
        string Name { get; set; }
        string Email { get; set; }
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
        static string[] SettingsCallBack(string name)
        {
            if (name == "MongoDb")
                return new[] { @"acrodblibtest", @"localhost", "27017", @"" };
            if (name == "MsSql")
                return new[] { @"server=OLEKSIY-DEVPC\SQLEXPRESS;Database=acrodbtest;Trusted_Connection=True;" };
            return new string[0];
        }
        static void Main()
        {
            DataContextFactory.SettingsCallback = SettingsCallBack;
            DataContextFactory.Instance.ScanAssembly(typeof (MsSqlDataContext).Assembly).ScanAssembly(
                typeof (MongoDbDataContext).Assembly);
            AcroDataContext.DefaultDataContext = DataContextFactory.Instance.Get("MsSql");
            AcroDataContext.ResetEntityMaps();
            AcroDataContext.ScanAssemblyForEntities(Assembly.GetExecutingAssembly());
            AcroDataContext.PerformMigrations();

            using (var manager = AcroDataContext.Go)
            {
                var usr = manager.Provide<IUserEntity>().Create();
                usr.Surname = DateTime.Now.ToLongDateString();
                usr.Name = DateTime.Now.ToLongTimeString();
                usr.Email = DateTime.Now.ToString();
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
