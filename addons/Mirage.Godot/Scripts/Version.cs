namespace Mirage
{
    public static class Version
    {
        public static readonly string Current = typeof(Version).Assembly.GetName().Version.ToString();
    }
}
