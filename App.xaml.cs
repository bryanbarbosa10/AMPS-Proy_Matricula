namespace AMPS
{
    public partial class App : Application
    {
        // Main database service
        private readonly DataBaseServices _dbService;

        public App(DataBaseServices dbService)
        {
            InitializeComponent();

            _dbService = dbService;

            // Load saved theme preference
            string savedTheme = Preferences.Get("AppTheme", "Sistema");

            UserAppTheme = savedTheme switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(
                new AppShell(_dbService)
            );
        }
    }
}