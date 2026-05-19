using System.Collections.ObjectModel;

namespace AMPS;

public partial class Matricula : ContentPage
{
    private readonly DataBaseServices _dbService;

    private string selectedPdfPath = "";

    public ObservableCollection<MatriculaItem> Matriculas { get; set; } = new();

    public Matricula(DataBaseServices dbService)
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
                "Debes seleccionar un perfil antes de usar Matrícula.",
                "OK"
            );

            await Shell.Current.GoToAsync("//ProfileManagement");
            return;
        }

        await LoadMatriculasAsync();
    }

    private async Task LoadMatriculasAsync()
    {
        Matriculas.Clear();

        var matriculas = await _dbService.GetMatriculasForActiveStudentAsync();

        foreach (var matricula in matriculas)
        {
            Matriculas.Add(matricula);
        }

        MatriculasCollectionView.ItemsSource = Matriculas;
    }

    private async void OnPickPdfClicked(object sender, EventArgs e)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Selecciona un PDF",
            FileTypes = FilePickerFileType.Pdf
        });

        if (result != null)
        {
            selectedPdfPath = result.FullPath;
            PdfLabel.Text = $"PDF: {result.FileName}";
        }
    }

    private async void OnSaveSemesterClicked(object sender, EventArgs e)
    {
        if (!ActiveProfileService.HasActiveProfile)
        {
            await DisplayAlert("Error", "No hay perfil activo.", "OK");
            return;
        }

        string semesterName = SemesterEntry.Text;

        if (string.IsNullOrWhiteSpace(semesterName))
        {
            await DisplayAlert("Error", "Escribe el nombre del semestre", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedPdfPath))
        {
            await DisplayAlert("Error", "Selecciona un PDF", "OK");
            return;
        }

        var nuevaMatricula = new MatriculaItem
        {
            Semestre = semesterName,
            Year = DateTime.Now.Year.ToString(),
            DateSaved = DateTime.Now
        };

        await _dbService.SaveMatriculaAsync(nuevaMatricula);

        await DisplayAlert(
            "Guardado",
            $"Semestre guardado correctamente\nPDF: {selectedPdfPath}",
            "OK"
        );

        SemesterEntry.Text = "";
        PdfLabel.Text = "Ningún PDF seleccionado";
        selectedPdfPath = "";

        await LoadMatriculasAsync();
    }

    private async void OnAddMatriculaClicked(object sender, EventArgs e)
    {
        if (!ActiveProfileService.HasActiveProfile)
        {
            await DisplayAlert("Error", "No hay perfil activo.", "OK");
            return;
        }

        string resultado = await DisplayActionSheet(
            "Nueva Matrícula",
            "Cancelar",
            null,
            "Verano 1",
            "Verano 2",
            "Próximo Semestre",
            "Semestre 1",
            "Semestre 2"
        );

        if (resultado == "Cancelar" || resultado == null)
            return;

        string year = await DisplayPromptAsync(
            "Año",
            "Escribe el año de la matrícula:",
            "Guardar",
            "Cancelar",
            "Ejemplo: 2026",
            maxLength: 4,
            keyboard: Keyboard.Numeric
        );

        if (string.IsNullOrWhiteSpace(year))
            return;

        var nuevaMatricula = new MatriculaItem
        {
            Semestre = resultado,
            Year = year,
            DateSaved = DateTime.Now
        };

        await _dbService.SaveMatriculaAsync(nuevaMatricula);

        await DisplayAlert(
            "Éxito",
            $"Matrícula guardada: {resultado} - {year}",
            "OK"
        );

        await LoadMatriculasAsync();
    }
}