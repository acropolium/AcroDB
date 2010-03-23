namespace AcroDB.Helpers
{
    public static class A
    {
        public static AcroDataContext AcroContext(this object objInterface)
        {
            return ((AcroEntity)objInterface).AcroContext;
        }
    }
}
