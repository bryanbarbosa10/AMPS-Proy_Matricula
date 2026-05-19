using System.Collections.ObjectModel;

namespace AMPS;

public partial class Promedio : ContentPage
{
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

        await CargarPromedioAsync();
    }

    private async Task CargarPromedioAsync()
    {
        MisNotas.Clear();
        HistorialGpa.Clear();

        var notas = await _dbService.GetGradesForActiveStudentAsync();

        foreach (var nota in notas)
        {
            MisNotas.Add(nota);
        }

        var historial = await _dbService.GetGpaHistoryForActiveStudentAsync();

        foreach (var item in historial)
        {
            HistorialGpa.Add(item);
        }

        CalcularGPA();
    }

    private void CalcularGPA()
    {
        if (MisNotas.Count == 0)
        {
            LblGpaTotal.Text = "0.00";
            LblCreditsTotal.Text = "Créditos completados: 0";
            return;
        }

        double honorPoints = MisNotas.Sum(n => n.PuntosDeHonor * n.Creditos);
        int totalCredits = MisNotas.Sum(n => n.Creditos);

        if (totalCredits == 0)
        {
            LblGpaTotal.Text = "0.00";
            LblCreditsTotal.Text = "Créditos completados: 0";
            return;
        }

        double gpa = honorPoints / totalCredits;

        LblGpaTotal.Text = gpa.ToString("F2");
        LblCreditsTotal.Text = $"Créditos completados: {totalCredits}";
    }
}