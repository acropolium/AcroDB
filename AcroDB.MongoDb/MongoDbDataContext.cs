using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using AcroDB.Attributes;
using NoRM;
using NoRM.Linq;

namespace AcroDB.MongoDb
{
    [AcroDbContext("MongoDb", typeof(MongoDbDataProvider<,>))]
    public class MongoDbDataContext : BaseDataContext
    {
        public abstract class MongoDbSession : IDisposable
        {
            readonly MongoQueryProvider _provider;
            protected MongoDbSession(string dbName) : this(dbName, @"localhost") { }
            protected MongoDbSession(string dbName, string server) : this(dbName, server, @"27017") { }
            protected MongoDbSession(string dbName, string server, string port) : this(dbName, server, port, @"") { }
            protected MongoDbSession(string dbName, string server, string port, string options)
            {
                _provider = new MongoQueryProvider(dbName, server, port, options);
            }
            public MongoQueryProvider Provider
            {
                get
                {
                    return _provider;
                }
            }
            public IQueryable<TEntity> GetTable<TEntity>()
            {
                return new MongoQuery<TEntity>(_provider);
            }

            public MongoCollection<TEntity> GetCollection<TEntity>() where TEntity : class, new()
            {
                return _provider.DB.GetCollection<TEntity>();
            }

            public void Add<T>(T item) where T : class, new()
            {
                _provider.DB.GetCollection<T>().Insert(item);
            }

            public void Update<T>(T item) where T : class, new()
            {
                _provider.DB.GetCollection<T>().UpdateOne(item, item);
            }

            public void Dispose()
            {
                _provider.Server.Dispose();
            }
        }

        private static Type _dcType;
        private static readonly object DcTypeLock = new object();
        private static readonly Random Random = new Random();
        private static string UniqueAppender
        {
            get
            {
                return String.Format("{0}{1}", Math.Abs(DateTime.Now.Ticks), Random.Next());
            }
        }
        private static Type GenerateDataContextType()
        {
            if (_dcType != null)
                return _dcType;
            lock (DcTypeLock)
            {
                if (_dcType != null)
                    return _dcType;
                var asmName = new AssemblyName("DynamicMongoDbDataContext" + UniqueAppender);
                var assemblyBldr = Thread.GetDomain().DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
                var moduleBldr = assemblyBldr.DefineDynamicModule(asmName.Name + UniqueAppender, true);
                var typeBuilder = moduleBldr.DefineType(asmName.Name + ".DataContext" + UniqueAppender,
                                                    TypeAttributes.Public |
                                                    TypeAttributes.Class |
                                                    TypeAttributes.AutoClass |
                                                    TypeAttributes.AnsiClass |
                                                    TypeAttributes.BeforeFieldInit |
                                                    TypeAttributes.AutoLayout, typeof(MongoDbSession));
                var il =
                    typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis,
                                                  new[]
                                                      {
                                                          typeof (string), typeof (string), typeof (string),
                                                          typeof (string)
                                                      }).GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ldarg_S, 4);
                il.Emit(OpCodes.Call, typeof(MongoDbSession).GetConstructor(new[] { typeof(string), typeof(string), typeof(string), typeof(string) }));
                il.Emit(OpCodes.Ret);
                
                il =
                    typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis,
                                                  new[]
                                                      {
                                                          typeof (string), typeof (string), typeof (string)
                                                      }).GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Call, typeof(MongoDbSession).GetConstructor(new[] { typeof(string), typeof(string), typeof(string) }));
                il.Emit(OpCodes.Ret);

                il =
                    typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis,
                                                  new[]
                                                      {
                                                          typeof (string), typeof (string)
                                                      }).GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, typeof(MongoDbSession).GetConstructor(new[] { typeof(string), typeof(string) }));
                il.Emit(OpCodes.Ret);

                il =
                    typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis,
                                                  new[]
                                                      {
                                                          typeof (string)
                                                      }).GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, typeof(MongoDbSession).GetConstructor(new[] { typeof(string) }));
                il.Emit(OpCodes.Ret);

                foreach (var type in AcroDataContext.GetInterfacesTypes<MongoDbDataContext>())
                {
                    var retType = typeof(IQueryable<>).MakeGenericType(new[] { type.Value });
                    var method = typeBuilder.DefineMethod("get_" + type.Value.Name, MethodAttributes.Private |
                                                                       MethodAttributes.SpecialName |
                                                                       MethodAttributes.HideBySig,
                                             retType, new Type[0]);
                    var currSetIl = method.GetILGenerator();
                    currSetIl.Emit(OpCodes.Ldarg_0);
                    currSetIl.Emit(OpCodes.Call, typeof(MongoDbSession).GetMethod("GetTable", new Type[0]));
                    currSetIl.Emit(OpCodes.Stloc_0);
                    var label = currSetIl.DefineLabel();
                    currSetIl.Emit(OpCodes.Br_S, label);
                    currSetIl.MarkLabel(label);
                    currSetIl.Emit(OpCodes.Ldloc_0);
                    currSetIl.Emit(OpCodes.Ret);
                    var property =
                            typeBuilder.DefineProperty(type.Value.Name,
                                                       PropertyAttributes.None,
                                                       retType,
                                                       null);
                    property.SetGetMethod(method);
                }
                _dcType = typeBuilder.CreateType();
            }
            return _dcType;
        }

        private readonly MongoDbSession _dc;

        public MongoDbDataContext(string[] parameters) : base(parameters)
        {
            _dc = (MongoDbSession)Activator.CreateInstance(GenerateDataContextType(), parameters);
        }

        public MongoDbSession Context
        {
            get
            {
                return _dc;
            }
        }

        public override void SubmitChanges()
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            _dc.Dispose();
        }
    }
}
