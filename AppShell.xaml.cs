namespace AMPS
{
    public partial class AppShell : Shell
    {
        private readonly DataBaseServices _dbService;

        public AppShell(DataBaseServices dbService)
        {
            InitializeComponent();

            _dbService = dbService;

            // Routes
            Routing.RegisterRoute(nameof(ProfileCreation), typeof(ProfileCreation));
            Routing.RegisterRoute(nameof(Dashboard), typeof(Dashboard));
            Routing.RegisterRoute(nameof(Matricula), typeof(Matricula));
            Routing.RegisterRoute(nameof(Promedio), typeof(Promedio));
            Routing.RegisterRoute(nameof(Secuencial), typeof(Secuencial));
            Routing.RegisterRoute(nameof(ProfileManagement), typeof(ProfileManagement));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

            Loaded += async (s, e) => await CheckInitialNavigationAsync();
        }

        private async Task CheckInitialNavigationAsync()
        {
            bool hasStudents = await _dbService.HasStudentsAsync();

            if (!hasStudents)
            {
                await Shell.Current.GoToAsync(nameof(ProfileCreation));
                return;
            }

            var savedStudentId = await ActiveProfileService.GetSavedActiveStudentIdAsync();

            if (!string.IsNullOrWhiteSpace(savedStudentId))
            {
                var student = await _dbService.GetStudentByStudentIdAsync(savedStudentId);

                if (student != null)
                {
                    await ActiveProfileService.SetActiveStudentAsync(student);
                }
            }

            await Shell.Current.GoToAsync("//Dashboard");
        }
    }
}