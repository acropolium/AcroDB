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
        [AcroColumnIsPrimaryKey]
        Guid ID { get; set; }
        string Name { get; set; }
    }

    public class UserEntityPrototype : MsSqlEntity, IUserEntity
    {
        private Guid _ID;
        private string _Name;
        public Guid ID
        {
            get
            {
                return _ID;
            }
            set
            {
                _ID = value;
            }
        }
        
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
            }
        }
    }

    class Program
    {
        static void Main()
        {
            AcroDataContext.DefaultDataProvider = typeof (MsSqlDataProvider<,>);
            AcroDataContext.DefaultDataContext = typeof(MsSqlDataContext);
            AcroDataContext.DefaultDataContextParams = new object[] { @"server=OLEKSIY-DEVPC\SQLEXPRESS;Database=acrodbtest;Trusted_Connection=True;" };
            AcroDataContext.ResetEntityMaps();
            AcroDataContext.ScanAssemblyForEntities(Assembly.GetExecutingAssembly());

            using (var manager = AcroDataContext.Go)
            {
                var usr = manager.Provide<IUserEntity>().Create();
                usr.Name = "sdf";
                manager.Provide<IUserEntity>().Save(usr);
                manager.SubmitChanges();
                foreach (var u in manager.Provide<IUserEntity>().GetFiltered(null, x => x.ID))
                {
                    Console.WriteLine(u.Name);
                }
            }
            Console.ReadKey();
        }
    }
}
