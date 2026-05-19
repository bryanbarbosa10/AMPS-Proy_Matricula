namespace AMPS;

public partial class SettingsPage : ContentPage
{
    private readonly DataBaseServices _dbService;
    private bool _isLoadingTheme = false;

    public SettingsPage(DataBaseServices dbService)
    {
        InitializeComponent();
        _dbService = dbService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _isLoadingTheme = true;

        string savedTheme = Preferences.Get("AppTheme", "Sistema");

        ThemePicker.SelectedItem = savedTheme;

        _isLoadingTheme = false;
    }

    private void OnThemeChanged(object sender, EventArgs e)
    {
        if (_isLoadingTheme)
            return;

        string selectedTheme = ThemePicker.SelectedItem?.ToString() ?? "Sistema";

        Preferences.Set("AppTheme", selectedTheme);

        Application.Current.UserAppTheme = selectedTheme switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    private async void OnResetSecuencialClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Reiniciar Secuencial",
            "Esta acción borrará todos los cursos del secuencial del perfil activo, sus checkmarks y las notas relacionadas. Esta acción no se puede deshacer.",
            "Entiendo, reiniciar",
            "Cancelar"
        );

        if (!confirm)
            return;

        await _dbService.ResetSecuencialForActiveStudentAsync();

        await ShowToastAsync("Secuencial reiniciado correctamente.");
    }

    private async void OnResetMatriculaClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Reiniciar Matrícula",
            "Esta acción borrará todas las matrículas guardadas del perfil activo y también eliminará sus archivos internos. Esta acción no se puede deshacer.",
            "Entiendo, reiniciar",
            "Cancelar"
        );

        if (!confirm)
            return;

        await _dbService.ResetMatriculaForActiveStudentAsync();

        await ShowToastAsync("Matrícula reiniciada correctamente.");
    }

    private async void OnResetPromedioClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Reiniciar Promedio",
            "Esta acción borrará todas las notas y el historial de GPA del perfil activo. También desmarcará los cursos completados para evitar inconsistencias. Esta acción no se puede deshacer.",
            "Entiendo, reiniciar",
            "Cancelar"
        );

        if (!confirm)
            return;

        await _dbService.ResetPromedioForActiveStudentAsync();

        await ShowToastAsync("Promedio reiniciado correctamente.");
    }

    private async Task ShowToastAsync(string message)
    {
        ToastLabel.Text = message;
        ToastFrame.Opacity = 0;
        ToastFrame.IsVisible = true;

        await ToastFrame.FadeTo(1, 200);
        await Task.Delay(1800);
        await ToastFrame.FadeTo(0, 300);

        ToastFrame.IsVisible = false;
    }
}