


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
                    // Default fonts
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Local database path
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "Student.db3");

            //TEMP: delete DB for testing first boot
            //if (File.Exists(dbPath))
            //{
            //    File.Delete(dbPath);
            //}

            // DB service
            builder.Services.AddSingleton(s => new DataBaseServices(dbPath));

            //Arreglando sequencial
            builder.Services.AddSingleton<CourseExtractionService>();

            // Pages
            builder.Services.AddTransient<ProfileCreation>();
            builder.Services.AddTransient<Dashboard>();
            builder.Services.AddTransient<Matricula>();
            builder.Services.AddTransient<Promedio>();
            builder.Services.AddTransient<Secuencial>();
            builder.Services.AddTransient<ProfileManagement>();
            builder.Services.AddTransient<SettingsPage>();

#if DEBUG
            // Debug log
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}