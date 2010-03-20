namespace AcroDB
{
    public class A
    {
        public static AcroDataContext Context(object obj)
        {
            return ((AcroEntity)obj).AcroContext;
        }
    }
}
