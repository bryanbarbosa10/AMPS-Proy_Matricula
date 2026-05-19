using System.Collections.ObjectModel;

namespace AMPS;

public partial class Secuencial : ContentPage
{
    private readonly DataBaseServices _dbService;

    // Analizar PDF o Word y extraer cursos
    private readonly CourseExtractionService _courseExtractionService;

    private bool _isLoadingData = false;

    private Course? _courseBeingEdited;

    private bool _isCreatingNewCourse = false;

    // Guarda el estado anterior de cada curso
    private readonly Dictionary<int, bool> _completedSnapshot = new();

    public ObservableCollection<Course> MiSecuencial { get; set; } = new();

    // Lista temporal de cursos
    public ObservableCollection<ExtractedCourse> CursosExtraidos { get; set; } = new();

    public Secuencial(
        DataBaseServices dbService,
        CourseExtractionService courseExtractionService)
    {
        InitializeComponent();
        _dbService = dbService;
        _courseExtractionService = courseExtractionService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!ActiveProfileService.HasActiveProfile)
        {
            await DisplayAlert(
                "Perfil requerido",
                "Debes seleccionar un perfil antes de usar Secuencial.",
                "OK"
            );

            await Shell.Current.GoToAsync("//ProfileManagement");
            return;
        }

        await CargarDatos();
    }

    private async void OnUploadDocumentClicked(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    // Se permite PDF y Word
                    { DevicePlatform.iOS, new[] { "com.adobe.pdf", "org.openxmlformats.wordprocessingml.document" } },
                    { DevicePlatform.Android, new[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
                    { DevicePlatform.WinUI, new[] { ".pdf", ".docx" } },
                });

            PickOptions options = new()
            {
                PickerTitle = "Selecciona tu documento secuencial",
                FileTypes = customFileType,
            };

            var result = await FilePicker.Default.PickAsync(options);

            if (result == null)
                return;

            FileNameLabel.Text = $"Archivo: {result.FileName}";

            // Aquí se analiza el archivo y se intentan extraer cursos automáticamente
            var extractedCourses = await _courseExtractionService.ExtractCoursesAsync(result);

            if (extractedCourses.Count == 0)
            {
                await DisplayAlert(
                    "Sin cursos detectados",
                    "No se detectaron cursos automáticamente. Puedes añadirlos manualmente o revisar el documento.",
                    "OK"
                );

                return;
            }

            CursosExtraidos.Clear();

            foreach (var course in extractedCourses)
            {
                CursosExtraidos.Add(course);
            }

            // Mostrar las cards editables
            ExtractedCoursesCollectionView.ItemsSource = CursosExtraidos;

            // Muestra como popup
            ExtractedCoursesOverlay.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo analizar el archivo: {ex.Message}", "OK");
        }
    }

    private void OnCancelExtractedCoursesClicked(object sender, EventArgs e)
    {
        CursosExtraidos.Clear();
        ExtractedCoursesOverlay.IsVisible = false;
    }

    private async Task CargarDatos()
    {
        _isLoadingData = true;

        var lista = await _dbService.GetCoursesForActiveStudentAsync();

        MiSecuencial.Clear();
        _completedSnapshot.Clear();

        foreach (var curso in lista)
        {
            MiSecuencial.Add(curso);
            _completedSnapshot[curso.Id] = curso.IsCompleted;
        }

        bool hasCourses = MiSecuencial.Count > 0;

        // Si ya hay cursos se esconde el botón de subir secuencial
        UploadSecuencialButton.IsVisible = !hasCourses;
        FileNameLabel.IsVisible = !hasCourses;

        // Si ya hay cursos, se muestra para añadir cursos manualmente
        AddManualCourseButton.IsVisible = hasCourses;

        // El botón de guardar solo aparece cuando el usuario cambia algún checkmark
        SaveProgressButton.IsVisible = false;

        _isLoadingData = false;
    }

    private async void OnAcceptExtractedCoursesClicked(object sender, EventArgs e)
    {
        if (!ActiveProfileService.HasActiveProfile)
        {
            await DisplayAlert("Error", "No hay perfil activo.", "OK");
            return;
        }

        var selectedCourses = CursosExtraidos
            .Where(c => c.IsSelected)
            .ToList();

        if (selectedCourses.Count == 0)
        {
            await DisplayAlert("AMPS", "No seleccionaste cursos para añadir.", "OK");
            return;
        }

        int savedCount = 0;

        foreach (var extracted in selectedCourses)
        {
            // Validación básica para evitar guardar cursos incompletos
            if (string.IsNullOrWhiteSpace(extracted.Codigo) ||
                string.IsNullOrWhiteSpace(extracted.Nombre) ||
                extracted.Creditos <= 0)
            {
                continue;
            }

            var course = new Course
            {
                Codigo = extracted.Codigo.Trim(),
                Nombre = extracted.Nombre.Trim(),
                Creditos = extracted.Creditos,
                IsCompleted = false
            };

            await _dbService.SaveCourseAsync(course);
            savedCount++;
        }

        CursosExtraidos.Clear();
        //esconder popup
        ExtractedCoursesOverlay.IsVisible = false;

        // Refresca la lista principal del secuencial luego de aceptar los cursos detectados
        await CargarDatos();

        await DisplayAlert(
            "AMPS",
            $"Se añadieron {savedCount} cursos al secuencial.",
            "OK"
        );
    }

    private async Task<string?> PreguntarYGuardarNotaAsync(Course curso)
    {
        string resultado = await DisplayActionSheet(
            $"Nota final para {curso.Nombre} ({curso.Codigo})",
            "Luego",
            null,
            "A",
            "B",
            "C",
            "D",
            "F"
        );

        if (string.IsNullOrWhiteSpace(resultado) || resultado == "Luego")
            return null;

        var nuevaNota = new Grade
        {
            Materia = curso.Nombre,
            Creditos = curso.Creditos,
            Calificacion = resultado
        };

        await _dbService.SaveGradeAsync(nuevaNota);

        return resultado;
    }

    private async void OnGuardarProgresoClicked(object sender, EventArgs e)
    {
        if (!ActiveProfileService.HasActiveProfile)
        {
            await DisplayAlert("Error", "No hay perfil activo.", "OK");
            return;
        }

        double previousGpa = await _dbService.CalculateCurrentGpaForActiveStudentAsync();

        var addedCoursesSummary = new List<string>();
        int totalCreditsAdded = 0;

        foreach (var curso in MiSecuencial)
        {
            bool wasCompletedBefore = _completedSnapshot.ContainsKey(curso.Id) &&
                                      _completedSnapshot[curso.Id];

            bool isNewlyCompleted = curso.IsCompleted && !wasCompletedBefore;

            bool wasUnchecked = !curso.IsCompleted && wasCompletedBefore;

            await _dbService.SaveCourseAsync(curso);

            if (isNewlyCompleted)
            {
                string? nota = await PreguntarYGuardarNotaAsync(curso);

                if (!string.IsNullOrWhiteSpace(nota))
                {
                    addedCoursesSummary.Add($"{curso.Nombre} ({curso.Codigo}) - Nota: {nota}");
                    totalCreditsAdded += curso.Creditos;
                }
            }

            if (wasUnchecked)
            {
                await _dbService.DeleteGradeForCourseAsync(curso);
                // Si usas historial agrupado, no borraremos tarjetas viejas automáticamente.
                // El historial representa cambios pasados, no el estado actual.
            }
        }

        if (addedCoursesSummary.Count > 0)
        {
            double newGpa = await _dbService.CalculateCurrentGpaForActiveStudentAsync();

            var history = new GpaHistory
            {
                PreviousGpa = previousGpa,
                NewGpa = newGpa,
                AddedCoursesSummary = string.Join("\n", addedCoursesSummary),
                TotalCreditsAdded = totalCreditsAdded,
                DateSaved = DateTime.Now
            };

            await _dbService.SaveGpaHistoryAsync(history);
        }

        await CargarDatos();

        await ShowToastAsync("Progreso guardado correctamente.");
    }

    private void OnEditCourseClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not Course curso)
            return;

        _courseBeingEdited = curso;
        _isCreatingNewCourse = false;

        EditCourseTitleLabel.Text = "Editar curso";
        DeleteCourseButton.IsVisible = true;

        EditCodigoEntry.Text = curso.Codigo;
        EditNombreEntry.Text = curso.Nombre;
        EditCreditosEntry.Text = curso.Creditos.ToString();

        EditCourseOverlay.IsVisible = true;
    }

    private void OnCancelEditCourseClicked(object sender, EventArgs e)
    {
        _courseBeingEdited = null;
        _isCreatingNewCourse = false;

        EditCourseTitleLabel.Text = "Editar curso";
        DeleteCourseButton.IsVisible = true;

        EditCodigoEntry.Text = string.Empty;
        EditNombreEntry.Text = string.Empty;
        EditCreditosEntry.Text = string.Empty;

        EditCourseOverlay.IsVisible = false;
    }

    private void OnAddManualCourseClicked(object sender, EventArgs e)
    {
        _courseBeingEdited = null;
        _isCreatingNewCourse = true;

        EditCourseTitleLabel.Text = "Añadir curso";
        DeleteCourseButton.IsVisible = false;

        EditCodigoEntry.Text = string.Empty;
        EditNombreEntry.Text = string.Empty;
        EditCreditosEntry.Text = string.Empty;

        EditCourseOverlay.IsVisible = true;
    }

    private async void OnSaveEditCourseClicked(object sender, EventArgs e)
    {
        string codigo = EditCodigoEntry.Text?.Trim() ?? string.Empty;
        string nombre = EditNombreEntry.Text?.Trim() ?? string.Empty;
        string creditosText = EditCreditosEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(codigo))
        {
            await DisplayAlert("Error", "El código del curso es requerido.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            await DisplayAlert("Error", "El nombre del curso es requerido.", "OK");
            return;
        }

        if (!int.TryParse(creditosText, out int creditos) || creditos <= 0)
        {
            await DisplayAlert("Error", "Los créditos deben ser un número mayor de 0.", "OK");
            return;
        }

        if (_isCreatingNewCourse)
        {
            var newCourse = new Course
            {
                Codigo = codigo,
                Nombre = nombre,
                Creditos = creditos,
                IsCompleted = false
            };

            await _dbService.SaveCourseAsync(newCourse);
        }
        else if (_courseBeingEdited != null)
        {
            _courseBeingEdited.Codigo = codigo;
            _courseBeingEdited.Nombre = nombre;
            _courseBeingEdited.Creditos = creditos;

            await _dbService.SaveCourseAsync(_courseBeingEdited);
        }

        _courseBeingEdited = null;
        _isCreatingNewCourse = false;

        EditCourseTitleLabel.Text = "Editar curso";
        DeleteCourseButton.IsVisible = true;

        EditCodigoEntry.Text = string.Empty;
        EditNombreEntry.Text = string.Empty;
        EditCreditosEntry.Text = string.Empty;

        EditCourseOverlay.IsVisible = false;

        await CargarDatos();

        await ShowToastAsync("Curso guardado correctamente.");
    }

    private async void OnDeleteCourseFromEditClicked(object sender, EventArgs e)
    {
        if (_isCreatingNewCourse)
            return;

        if (_courseBeingEdited == null)
            return;

        bool confirm = await DisplayAlert(
            "Borrar curso",
            $"¿Seguro que deseas borrar {_courseBeingEdited.Nombre} ({_courseBeingEdited.Codigo})?",
            "Sí, borrar",
            "Cancelar"
        );

        if (!confirm)
            return;

        // Si tenía nota en promedio, también se elimina
        await _dbService.DeleteGradeForCourseAsync(_courseBeingEdited);

        await _dbService.DeleteCourseAsync(_courseBeingEdited);

        _courseBeingEdited = null;
        _isCreatingNewCourse = false;

        EditCourseTitleLabel.Text = "Editar curso";
        DeleteCourseButton.IsVisible = true;

        EditCodigoEntry.Text = string.Empty;
        EditNombreEntry.Text = string.Empty;
        EditCreditosEntry.Text = string.Empty;

        EditCourseOverlay.IsVisible = false;

        await CargarDatos();

        await ShowToastAsync("Curso eliminado correctamente.");
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

    private void OnCourseCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isLoadingData)
            return;

        // Cuando el usuario marca o desmarca un curso, aparece el botón de guardar progreso
        SaveProgressButton.IsVisible = true;
    }

}