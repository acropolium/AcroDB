using System;

namespace AcroDB.Helpers
{
    public static class A
    {
        public static IDataContextProvider AcroContext(this object objInterface)
        {
            if (!(objInterface is AcroEntity))
                throw new Exception("Object is not AcroEntity");
            var context = ((AcroEntity)objInterface).DataContextProvider;
            if (context == null)
                throw new Exception("AcroContext was not attached to object");
            return context;
        }
    }
}
