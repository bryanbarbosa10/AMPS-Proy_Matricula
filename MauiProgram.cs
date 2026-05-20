using Microsoft.Extensions.Logging;

namespace AMPS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf","OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf","OpenSansSemibold");
                });

            // Local SQLite database path
            string databasePath = Path.Combine(
                FileSystem.AppDataDirectory,
                "Student.db3"
            );

            // TEMP: delete database for first boot testing
            // if (File.Exists(databasePath))
            // {
            //     File.Delete(databasePath);
            // }

            RegisterServices(builder, databasePath);

#if DEBUG
            // Enable debug logs in development
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static void RegisterServices(
            MauiAppBuilder builder,
            string databasePath)
        {
            // Database service
            builder.Services.AddSingleton(
                serviceProvider => new DataBaseServices(databasePath)
            );

            // Course extraction service
            builder.Services.AddSingleton<CourseExtractionService>();

            // Application pages
            builder.Services.AddTransient<ProfileCreation>();
            builder.Services.AddTransient<Dashboard>();
            builder.Services.AddTransient<Matricula>();
            builder.Services.AddTransient<Promedio>();
            builder.Services.AddTransient<Secuencial>();
            builder.Services.AddTransient<ProfileManagement>();
            builder.Services.AddTransient<SettingsPage>();
        }
    }
}