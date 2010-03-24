using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using AcroDB.Attributes;
using SubSonic.SqlGeneration.Schema;

namespace AcroDB.EntityFactory
{
    public class EntityBuilder
    {
        #region Safe threaded singleton
        private EntityBuilder() { }
        public static EntityBuilder Instance
        {
            get
            {
                return ThreadSafeSingletonHelper.SafeInstance;
            }
        }
        private class ThreadSafeSingletonHelper
        {
            static ThreadSafeSingletonHelper() { }
            internal static readonly EntityBuilder SafeInstance = new EntityBuilder();
        }
        #endregion

        private ModuleBuilder _moduleBldr;
        private AssemblyBuilder _assemblyBldr;
        private static readonly Random Random = new Random();
        private static string UniqueAppender
        {
            get
            {
                return String.Format("{0}{1}", Math.Abs(DateTime.Now.Ticks), Random.Next());
            }
        }
        private ModuleBuilder ModuleBuilder
        {
            get
            {
                if (_moduleBldr == null)
                {
                    var asmName = new AssemblyName("DynamicEntities" + UniqueAppender);
                    _assemblyBldr = Thread.GetDomain().DefineDynamicAssembly(asmName,
#if SAVE_ASSEMBLY
                                                                              AssemblyBuilderAccess.RunAndSave, HostingEnvironment.MapPath("~/App_Data/")
#else
 AssemblyBuilderAccess.Run
#endif
);
#if SAVE_ASSEMBLY
                    _assemblyBldr = assemblyBldr;
                    _moduleBldr = assemblyBldr.DefineDynamicModule(asmName.Name, asmName + ".dll", true);
#else
                    _moduleBldr = _assemblyBldr.DefineDynamicModule(asmName.Name + UniqueAppender, true);
#endif
                }
                return _moduleBldr;
            }
        }

        private Type BuildEntity(Type interfaceType, Type baseEntityType, string name, Action<Type, TypeBuilder> beforeCreateTypeCallback)
        {
            var attr = interfaceType.GetCustomAttributes(typeof(AcroDbEntityAttribute), true).FirstOrDefault() as AcroDbEntityAttribute;
            if (attr == null)
                return null;
            var parentType = (attr.ParentType == typeof (AcroEntity))
                                 ? (baseEntityType ?? attr.ParentType)
                                 : attr.ParentType;
            var interfaces = new List<Type>(attr.InterfacesToImplement);
            interfaces.Insert(0, interfaceType);
            var typeBuilder = ModuleBuilder.DefineType(String.Format("{0}.{1}", _assemblyBldr.GetName().Name, name),
                                                       TypeAttributes.Public |
                                                       TypeAttributes.Class |
                                                       TypeAttributes.AutoClass |
                                                       TypeAttributes.AnsiClass |
                                                       TypeAttributes.BeforeFieldInit |
                                                       TypeAttributes.AutoLayout, parentType,
                                                       interfaces.ToArray());
            typeBuilder.SetCustomAttribute(
                new CustomAttributeBuilder(
                    typeof (SubSonicTableNameOverrideAttribute).GetConstructor(new[] {typeof (string)}),
                    new object[] {name}));
            foreach (var property in interfaceType.GetProperties())
                AddAutoProperty(typeBuilder, property);
            if (beforeCreateTypeCallback != null)
                beforeCreateTypeCallback(interfaceType, typeBuilder);
            return typeBuilder.CreateType();
        }

        private const MethodAttributes GetSetAttr =
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig |
            MethodAttributes.SpecialName;

        private static void AddAutoProperty(TypeBuilder builder, PropertyInfo propertyInfo)
        {
            var propertyName = propertyInfo.Name;
            var rtype = propertyInfo.PropertyType;
            var field = builder.DefineField("_" + propertyName, rtype,
                                                FieldAttributes.Private);
            var currGetPropMthdBldr =
                builder.DefineMethod(propertyInfo.GetGetMethod().Name,
                                     GetSetAttr,
                                     rtype,
                                     Type.EmptyTypes);
            var currGetIl = currGetPropMthdBldr.GetILGenerator();
            currGetIl.Emit(OpCodes.Ldarg_0);
            currGetIl.Emit(OpCodes.Ldfld, field);
            currGetIl.Emit(OpCodes.Ret);

            var currSetPropMthdBldr =
                builder.DefineMethod(propertyInfo.GetSetMethod().Name,
                                     GetSetAttr,
                                     null,
                                     new[] {rtype});

            var currSetIl = currSetPropMthdBldr.GetILGenerator();
            if (!rtype.IsGenericType || !typeof(Nullable<>).MakeGenericType(rtype.GetGenericArguments()[0]).Equals(rtype))
            {
                currSetIl.Emit(OpCodes.Nop);
                var endOfMethod = currSetIl.DefineLabel();

                var methodInfo = rtype.GetMethod("op_Inequality");
                if (methodInfo != null)
                {
                    currSetIl.Emit(OpCodes.Ldarg_0);
                    currSetIl.Emit(OpCodes.Ldfld, field);
                    currSetIl.Emit(OpCodes.Ldarg_1);
                    currSetIl.Emit(OpCodes.Call, methodInfo);
                    currSetIl.Emit(OpCodes.Ldc_I4_0);
                    currSetIl.Emit(OpCodes.Ceq);
                    currSetIl.Emit(OpCodes.Brtrue_S, endOfMethod);
                }
                else
                {
                    currSetIl.Emit(OpCodes.Ldarg_0);
                    currSetIl.Emit(OpCodes.Ldfld, field);
                    currSetIl.Emit(OpCodes.Ldarg_1);
                    currSetIl.Emit(OpCodes.Ceq);
                    currSetIl.Emit(OpCodes.Brtrue_S, endOfMethod);
                }
                /*
                currSetIl.Emit(OpCodes.Ldarg_0);
                currSetIl.Emit(OpCodes.Ldstr, propertyName);
                currSetIl.Emit(OpCodes.Callvirt,
                               table.EntityBaseType.GetMethod("SendPropertyChanging",
                                                              BindingFlags.Instance | BindingFlags.NonPublic));*/
                currSetIl.Emit(OpCodes.Ldarg_0);
                currSetIl.Emit(OpCodes.Ldarg_1);
                currSetIl.Emit(OpCodes.Stfld, field);
                /*
                currSetIl.Emit(OpCodes.Ldarg_0);
                currSetIl.Emit(OpCodes.Ldstr, propertyName);
                currSetIl.Emit(OpCodes.Callvirt,
                               table.EntityBaseType.GetMethod("SendPropertyChanged",
                                                              BindingFlags.Instance | BindingFlags.NonPublic));*/
                currSetIl.MarkLabel(endOfMethod);
            }
            else
            {
                var setInitializedMethod =
                    typeof(AcroEntity).GetMethod("SetNullableItem",
                                                      BindingFlags.Instance | BindingFlags.NonPublic).
                        MakeGenericMethod(rtype.GetGenericArguments()[0]);
                currSetIl.Emit(OpCodes.Ldarg_0);
                currSetIl.Emit(OpCodes.Ldstr, propertyName);
                currSetIl.Emit(OpCodes.Ldstr, "_" + propertyName);
                currSetIl.Emit(OpCodes.Ldarg_1);
                currSetIl.Emit(OpCodes.Call, setInitializedMethod);
            }
            currSetIl.Emit(OpCodes.Ret);

            var property = builder.DefineProperty(propertyName,
                                                  PropertyAttributes.None,
                                                  rtype,
                                                  null);
            property.SetGetMethod(currGetPropMthdBldr);
            property.SetSetMethod(currSetPropMthdBldr);
            builder.DefineMethodOverride(currGetPropMthdBldr, propertyInfo.GetGetMethod());
            builder.DefineMethodOverride(currSetPropMthdBldr, propertyInfo.GetSetMethod());

            foreach (var attribute in propertyInfo.GetCustomAttributes(true))
            {
                if (attribute is AcroColumnIsPrimaryKeyAttribute)
                {
                    property.SetCustomAttribute(
                        new CustomAttributeBuilder(typeof (SubSonicPrimaryKeyAttribute).GetConstructor(new Type[0]),
                                                   new object[0]));
                }
                else if (attribute is AcroColumnIgnoreStorageAttribute)
                {
                    property.SetCustomAttribute(
                        new CustomAttributeBuilder(typeof (SubSonicIgnoreAttribute).GetConstructor(new Type[0]),
                                                   new object[0]));
                }
                else if (attribute is AcroColumnStringLongAttribute)
                {
                    property.SetCustomAttribute(
                        new CustomAttributeBuilder(typeof (SubSonicLongStringAttribute).GetConstructor(new Type[0]),
                                                   new object[0]));
                }
                else if (attribute is AcroColumnStringCanBeNullAttribute)
                {
                    property.SetCustomAttribute(
                        new CustomAttributeBuilder(typeof(SubSonicNullStringAttribute).GetConstructor(new Type[0]),
                                                   new object[0]));
                }
                else if (attribute is AcroColumnStringLengthAttribute)
                {
                    property.SetCustomAttribute(
                        new CustomAttributeBuilder(
                            typeof (SubSonicStringLengthAttribute).GetConstructor(new[] {typeof (int)}),
                            new object[] {((AcroColumnStringLengthAttribute) attribute).Length}));
                }
                else if (attribute is AcroColumnNumericPrecisionAttribute)
                {
                    property.SetCustomAttribute(
                        new CustomAttributeBuilder(
                            typeof (SubSonicNumericPrecisionAttribute).GetConstructor(new[] {typeof (int), typeof (int)}),
                            new object[]
                                {
                                    ((AcroColumnNumericPrecisionAttribute) attribute).Precision,
                                    ((AcroColumnNumericPrecisionAttribute) attribute).Scale
                                }));
                }
            }
        }

        public static Type Build(Type interfaceType, Type baseEntityType, string customName, Action<Type, TypeBuilder> beforeCreateTypeCallback)
        {
            return Instance.BuildEntity(interfaceType, baseEntityType,
                                        String.IsNullOrEmpty(customName)
                                            ? ((interfaceType.Name[0] == 'I')
                                                   ? interfaceType.Name.Substring(1)
                                                   : interfaceType.Name)
                                            : customName,
                                            beforeCreateTypeCallback);
        }
    }
}