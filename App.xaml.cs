namespace AMPS
{
    public partial class App : Application
    {
        private readonly DataBaseServices _dbService;

        public App(DataBaseServices dbService)
        {
            InitializeComponent();
            string savedTheme = Preferences.Get("AppTheme", "Sistema");

            UserAppTheme = savedTheme switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
            _dbService = dbService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell(_dbService));
        }
    }
}