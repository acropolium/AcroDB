using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using AcroDB.Attributes;

namespace AcroDB.MsSql
{
    [AutoMigrationSupported("System.Data.SqlClient")]
    public class MsSqlDataContext : BaseDataContext
    {
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
                var asmName = new AssemblyName("DynamicMsSqlDataContext" + UniqueAppender);
                var assemblyBldr = Thread.GetDomain().DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
                var moduleBldr = assemblyBldr.DefineDynamicModule(asmName.Name + UniqueAppender, true);
                var typeBuilder = moduleBldr.DefineType(asmName.Name + ".DataContext" + UniqueAppender,
                                                    TypeAttributes.Public |
                                                    TypeAttributes.Class |
                                                    TypeAttributes.AutoClass |
                                                    TypeAttributes.AnsiClass |
                                                    TypeAttributes.BeforeFieldInit |
                                                    TypeAttributes.AutoLayout, typeof(DataContext));
                var ctor1 = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { typeof(string), typeof(XmlMappingSource) });
                var il = ctor1.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, typeof(DataContext).GetConstructor(new[] { typeof(string), typeof(MappingSource) }));
                il.Emit(OpCodes.Ret);
                foreach (var type in AcroDataContext.GetInterfacesTypes<MsSqlDataContext>())
                {
                    var retType = typeof(Table<>).MakeGenericType(new[] { type.Value });
                    var method = typeBuilder.DefineMethod("get_" + type.Value.Name, MethodAttributes.Private |
                                                                       MethodAttributes.SpecialName |
                                                                       MethodAttributes.HideBySig,
                                             retType, new Type[0]);
                    var currSetIl = method.GetILGenerator();
                    currSetIl.Emit(OpCodes.Ldarg_0);
                    currSetIl.Emit(OpCodes.Call, typeof(DataContext).GetMethod("GetTable", new Type[0]));
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

        private readonly DataContext _dc;

        public MsSqlDataContext(string[] parameters) : base(parameters)
        {
            _dc = (DataContext) Activator.CreateInstance(GenerateDataContextType(), parameters[0], MappingSource);
        }

        private static XmlMappingSource _source;
        private static readonly object XmlGeneratorLock = new object();
        private static XmlMappingSource MappingSource
        {
            get
            {
                if (_source != null)
                    return _source;
                lock (XmlGeneratorLock)
                {
                    if (_source != null)
                        return _source;
                    _source = XmlMappingSource.FromXml(GenerateXmlMap());
                }
                return _source;
            }
        }

        private static string GetValidName(string name)
        {
            return String.Format("{0}{1}", name.Substring(0, name.Length - 1), name.EndsWith("y") ? "ies" : "s");
        }

        private static string GenerateXmlMap()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            sb.AppendLine("<Database Name=\"MemoryDB\" xmlns=\"http://schemas.microsoft.com/linqtosql/mapping/2007\">");
            foreach (var type in AcroDataContext.GetInterfacesTypes<MsSqlDataContext>())
            {
                var interfaceType = type.Key;
                var entityType = type.Value;
                var dbAttr =
                    (AcroDbEntityAttribute)
                    interfaceType.GetCustomAttributes(typeof (AcroDbEntityAttribute), true).FirstOrDefault();
                if (dbAttr == null)
                    continue;
                var name = GetValidName(String.IsNullOrEmpty(dbAttr.Name)
                                            ? (interfaceType.Name[0] == 'I'
                                                   ? interfaceType.Name.Substring(1)
                                                   : interfaceType.Name)
                                            : dbAttr.Name);
                sb.AppendFormat("<Table Name=\"{0}\" Member=\"{1}\">", name, entityType.FullName);
                sb.AppendLine();
                sb.AppendFormat("<Type Name=\"{0}\">", entityType.FullName);
                sb.AppendLine();
                var columns = from pi in interfaceType.GetProperties()
                              where pi.GetCustomAttributes(typeof (AcroColumnIgnoreStorageAttribute), true).Length == 0
                              select pi;
                foreach (var column in columns)
                {
                    sb.Append("<Column");
                    sb.AppendFormat(" Name=\"{0}\" Member=\"{0}\" Storage=\"_{0}\"", column.Name);
                    if (column.GetCustomAttributes(typeof(AcroColumnIsPrimaryKeyAttribute), true).Length > 0 || column.Name.Equals("ID") || column.Name.Equals("Key") || column.Name.Equals(entityType.Name + "ID"))
                        sb.Append(" IsPrimaryKey=\"true\"");
                    if (column.PropertyType.IsGenericType && column.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        sb.Append(" CanBeNull=\"true\"");
                    sb.AppendLine(" />");
                }
                sb.AppendLine("</Type>");
                sb.AppendLine("</Table>");
            }
            sb.AppendLine("</Database>");
            return sb.ToString();
        }

        internal DataContext Context
        {
            get { return _dc; }
        }

        public override void SubmitChanges()
        {
            _dc.SubmitChanges();
        }

        public override void Dispose()
        {
            base.Dispose();
            _dc.Dispose();
        }
    }
}
