namespace RenPyReader
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Current!.PageDisappearing += static (s, e) => ClearCacheFolder();
        }

        private static void ClearCacheFolder()
        {
            string cacheDir = FileSystem.CacheDirectory;
            if (Directory.Exists(cacheDir))
            {
                try
                {
                    Directory.Delete(cacheDir, true);
                    Directory.CreateDirectory(cacheDir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "RenPyReader" };
        }
    }
}
