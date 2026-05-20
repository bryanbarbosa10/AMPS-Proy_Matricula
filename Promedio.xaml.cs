using System.Collections.ObjectModel;

namespace AMPS;

public partial class Promedio : ContentPage
{
    // Main database service
    private readonly DataBaseServices _dbService;

    public ObservableCollection<Grade> MisNotas { get; set; } = new();

    public ObservableCollection<GpaHistory> HistorialGpa { get; set; } = new();

    public Promedio(DataBaseServices dbService)
    {
        InitializeComponent();

        _dbService = dbService;

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!ActiveProfileService.HasActiveProfile)
        {
            await DisplayAlert(
                "Perfil requerido",
                "Debes seleccionar un perfil antes de usar Promedio.",
                "OK"
            );

            await Shell.Current.GoToAsync("//ProfileManagement");

            return;
        }

        await LoadGpaAsync();
    }

    private async Task LoadGpaAsync()
    {
        MisNotas.Clear();
        HistorialGpa.Clear();

        await LoadGradesAsync();
        await LoadGpaHistoryAsync();

        CalculateGpa();
    }

    private async Task LoadGradesAsync()
    {
        List<Grade> grades = await _dbService.GetGradesForActiveStudentAsync();

        foreach (Grade grade in grades)
        {
            MisNotas.Add(grade);
        }
    }

    private async Task LoadGpaHistoryAsync()
    {
        List<GpaHistory> history =
            await _dbService.GetGpaHistoryForActiveStudentAsync();

        foreach (GpaHistory item in history)
        {
            HistorialGpa.Add(item);
        }
    }

    // Calculate GPA from saved grades
    private void CalculateGpa()
    {
        if (MisNotas.Count == 0)
        {
            LblGpaTotal.Text = "0.00";
            LblCreditsTotal.Text = "Créditos completados: 0";

            return;
        }

        double honorPoints = MisNotas.Sum(
            grade => grade.PuntosDeHonor * grade.Creditos
        );

        int totalCredits = MisNotas.Sum(
            grade => grade.Creditos
        );

        if (totalCredits == 0)
        {
            LblGpaTotal.Text = "0.00";
            LblCreditsTotal.Text = "Créditos completados: 0";

            return;
        }

        double gpa = honorPoints / totalCredits;

        LblGpaTotal.Text = gpa.ToString("F2");

        LblCreditsTotal.Text =
            $"Créditos completados: {totalCredits}";
    }
}