using System.Collections.ObjectModel;

namespace AMPS;

public partial class Secuencial : ContentPage
{
    // Main database service
    private readonly DataBaseServices _dbService;

    // Service used to extract courses from PDF or Word files
    private readonly CourseExtractionService _courseExtractionService;

    // Prevents checkbox events while loading data
    private bool _isLoadingData = false;

    // Tracks if overlay is creating a new course
    private bool _isCreatingNewCourse = false;

    // Current course being edited
    private Course? _courseBeingEdited;

    // Stores previous completion state of each course
    private readonly Dictionary<int, bool> _completedSnapshot = new();

    // Main course list shown in secuencial
    public ObservableCollection<Course> MiSecuencial { get; set; } = new();

    // Temporary extracted courses before saving
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

        // Prevent entering secuencial without active profile
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

        await LoadDataAsync();
    }

    // Load all courses for current active student
    private async Task LoadDataAsync()
    {
        _isLoadingData = true;

        List<Course> courses =
            await _dbService.GetCoursesForActiveStudentAsync();

        MiSecuencial.Clear();

        _completedSnapshot.Clear();

        foreach (Course course in courses)
        {
            MiSecuencial.Add(course);

            // Save previous completion state
            _completedSnapshot[course.Id] = course.IsCompleted;
        }

        bool hasCourses = MiSecuencial.Count > 0;

        // Upload button only appears if no courses exist
        UploadSecuencialButton.IsVisible = !hasCourses;

        FileNameLabel.IsVisible = !hasCourses;

        // Manual course button appears after courses exist
        AddManualCourseButton.IsVisible = hasCourses;

        // Save progress button only appears after changes
        SaveProgressButton.IsVisible = false;

        _isLoadingData = false;
    }

    // Upload and analyze secuencial document
    private async void OnUploadDocumentClicked(object sender, EventArgs e)
    {
        try
        {
            // Allow PDF and Word files
            FilePickerFileType customFileType = new(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    {
                        DevicePlatform.iOS,
                        new[]
                        {
                            "com.adobe.pdf",
                            "org.openxmlformats.wordprocessingml.document"
                        }
                    },

                    {
                        DevicePlatform.Android,
                        new[]
                        {
                            "application/pdf",
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                        }
                    },

                    {
                        DevicePlatform.WinUI,
                        new[] { ".pdf", ".docx" }
                    }
                });

            PickOptions options = new()
            {
                PickerTitle = "Selecciona tu documento secuencial",
                FileTypes = customFileType
            };

            FileResult? result =
                await FilePicker.Default.PickAsync(options);

            if (result == null)
                return;

            FileNameLabel.Text = $"Archivo: {result.FileName}";

            // Analyze file and extract possible courses
            List<ExtractedCourse> extractedCourses =
                await _courseExtractionService.ExtractCoursesAsync(result);

            if (extractedCourses.Count == 0)
            {
                await DisplayAlert(
                    "Sin cursos detectados",
                    "No se detectaron cursos automáticamente. Puedes añadirlos manualmente o revisar el documento.",
                    "OK"
                );

                return;
            }

            LoadExtractedCourses(extractedCourses);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error",
                $"No se pudo analizar el archivo: {ex.Message}",
                "OK"
            );
        }
    }

    // Load extracted courses into temporary overlay
    private void LoadExtractedCourses(
        List<ExtractedCourse> extractedCourses)
    {
        CursosExtraidos.Clear();

        foreach (ExtractedCourse course in extractedCourses)
        {
            CursosExtraidos.Add(course);
        }

        // Show editable extracted courses popup
        ExtractedCoursesCollectionView.ItemsSource = CursosExtraidos;

        ExtractedCoursesOverlay.IsVisible = true;
    }

    private void OnCancelExtractedCoursesClicked(
        object sender,
        EventArgs e)
    {
        CursosExtraidos.Clear();

        ExtractedCoursesOverlay.IsVisible = false;
    }

    // Save selected extracted courses into database
    private async void OnAcceptExtractedCoursesClicked(
        object sender,
        EventArgs e)
    {
        if (!ActiveProfileService.HasActiveProfile)
        {
            await DisplayAlert(
                "Error",
                "No hay perfil activo.",
                "OK"
            );

            return;
        }

        List<ExtractedCourse> selectedCourses =
            CursosExtraidos
                .Where(course => course.IsSelected)
                .ToList();

        if (selectedCourses.Count == 0)
        {
            await DisplayAlert(
                "AMPS",
                "No seleccionaste cursos para añadir.",
                "OK"
            );

            return;
        }

        int savedCount =
            await SaveExtractedCoursesAsync(selectedCourses);

        CursosExtraidos.Clear();

        // Hide popup after saving
        ExtractedCoursesOverlay.IsVisible = false;

        await LoadDataAsync();

        await DisplayAlert(
            "AMPS",
            $"Se añadieron {savedCount} cursos al secuencial.",
            "OK"
        );
    }

    // Save valid extracted courses
    private async Task<int> SaveExtractedCoursesAsync(
        List<ExtractedCourse> selectedCourses)
    {
        int savedCount = 0;

        foreach (ExtractedCourse extractedCourse in selectedCourses)
        {
            // Prevent invalid or incomplete courses
            if (!IsValidExtractedCourse(extractedCourse))
                continue;

            var course = new Course
            {
                Codigo = extractedCourse.Codigo.Trim(),
                Nombre = extractedCourse.Nombre.Trim(),
                Creditos = extractedCourse.Creditos,
                IsCompleted = false
            };

            await _dbService.SaveCourseAsync(course);

            savedCount++;
        }

        return savedCount;
    }

    // Validate extracted course data
    private bool IsValidExtractedCourse(ExtractedCourse course)
    {
        return
            !string.IsNullOrWhiteSpace(course.Codigo) &&
            !string.IsNullOrWhiteSpace(course.Nombre) &&
            course.Creditos > 0;
    }

    // Save completion progress and GPA changes
    private async void OnGuardarProgresoClicked(
        object sender,
        EventArgs e)
    {
        if (!ActiveProfileService.HasActiveProfile)
        {
            await DisplayAlert(
                "Error",
                "No hay perfil activo.",
                "OK"
            );

            return;
        }

        // Save GPA before modifications
        double previousGpa =
            await _dbService.CalculateCurrentGpaForActiveStudentAsync();

        var addedCoursesSummary = new List<string>();

        int totalCreditsAdded = 0;

        foreach (Course course in MiSecuencial)
        {
            bool wasCompletedBefore =
                _completedSnapshot.ContainsKey(course.Id) &&
                _completedSnapshot[course.Id];

            // Detect newly completed course
            bool isNewlyCompleted =
                course.IsCompleted && !wasCompletedBefore;

            // Detect unchecked course
            bool wasUnchecked =
                !course.IsCompleted && wasCompletedBefore;

            await _dbService.SaveCourseAsync(course);

            // Ask for final grade if course was completed
            if (isNewlyCompleted)
            {
                string? grade =
                    await AskAndSaveGradeAsync(course);

                if (!string.IsNullOrWhiteSpace(grade))
                {
                    addedCoursesSummary.Add(
                        $"{course.Nombre} ({course.Codigo}) - Nota: {grade}"
                    );

                    totalCreditsAdded += course.Creditos;
                }
            }

            // Remove grade if course becomes unchecked
            if (wasUnchecked)
            {
                await _dbService.DeleteGradeForCourseAsync(course);
            }
        }

        // Save GPA history card if changes happened
        await SaveGpaHistoryIfNeededAsync(
            previousGpa,
            addedCoursesSummary,
            totalCreditsAdded
        );

        await LoadDataAsync();

        await ShowToastAsync(
            "Progreso guardado correctamente."
        );
    }

    // Ask user for final grade and save it
    private async Task<string?> AskAndSaveGradeAsync(Course course)
    {
        string result = await DisplayActionSheet(
            $"Nota final para {course.Nombre} ({course.Codigo})",
            "Luego",
            null,
            "A",
            "B",
            "C",
            "D",
            "F"
        );

        if (string.IsNullOrWhiteSpace(result) ||
            result == "Luego")
        {
            return null;
        }

        var grade = new Grade
        {
            Materia = course.Nombre,
            Creditos = course.Creditos,
            Calificacion = result
        };

        await _dbService.SaveGradeAsync(grade);

        return result;
    }

    // Save GPA history entry if new courses were completed
    private async Task SaveGpaHistoryIfNeededAsync(
        double previousGpa,
        List<string> addedCoursesSummary,
        int totalCreditsAdded)
    {
        if (addedCoursesSummary.Count == 0)
            return;

        double newGpa =
            await _dbService.CalculateCurrentGpaForActiveStudentAsync();

        var history = new GpaHistory
        {
            PreviousGpa = previousGpa,
            NewGpa = newGpa,
            AddedCoursesSummary =
                string.Join("\n", addedCoursesSummary),

            TotalCreditsAdded = totalCreditsAdded,

            DateSaved = DateTime.Now
        };

        await _dbService.SaveGpaHistoryAsync(history);
    }

    // Open overlay to edit existing course
    private void OnEditCourseClicked(object sender, EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter is not Course course)
        {
            return;
        }

        _courseBeingEdited = course;

        _isCreatingNewCourse = false;

        OpenCourseOverlay(
            "Editar curso",
            course.Codigo,
            course.Nombre,
            course.Creditos.ToString(),
            true
        );
    }

    // Open overlay to create manual course
    private void OnAddManualCourseClicked(
        object sender,
        EventArgs e)
    {
        _courseBeingEdited = null;

        _isCreatingNewCourse = true;

        OpenCourseOverlay(
            "Añadir curso",
            string.Empty,
            string.Empty,
            string.Empty,
            false
        );
    }

    // Configure and show edit overlay
    private void OpenCourseOverlay(
        string title,
        string code,
        string name,
        string credits,
        bool showDeleteButton)
    {
        EditCourseTitleLabel.Text = title;

        DeleteCourseButton.IsVisible = showDeleteButton;

        EditCodigoEntry.Text = code;
        EditNombreEntry.Text = name;
        EditCreditosEntry.Text = credits;

        EditCourseOverlay.IsVisible = true;
    }

    private void OnCancelEditCourseClicked(
        object sender,
        EventArgs e)
    {
        CloseCourseOverlay();
    }

    // Save edited or manually created course
    private async void OnSaveEditCourseClicked(
        object sender,
        EventArgs e)
    {
        string code =
            EditCodigoEntry.Text?.Trim() ?? string.Empty;

        string name =
            EditNombreEntry.Text?.Trim() ?? string.Empty;

        string creditsText =
            EditCreditosEntry.Text?.Trim() ?? string.Empty;

        if (!await ValidateCourseFormAsync(
            code,
            name,
            creditsText))
        {
            return;
        }

        int credits = int.Parse(creditsText);

        if (_isCreatingNewCourse)
        {
            await CreateManualCourseAsync(
                code,
                name,
                credits
            );
        }
        else if (_courseBeingEdited != null)
        {
            await UpdateCourseAsync(
                code,
                name,
                credits
            );
        }

        CloseCourseOverlay();

        await LoadDataAsync();

        await ShowToastAsync(
            "Curso guardado correctamente."
        );
    }

    // Validate manual/edit course form
    private async Task<bool> ValidateCourseFormAsync(
        string code,
        string name,
        string creditsText)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            await DisplayAlert(
                "Error",
                "El código del curso es requerido.",
                "OK"
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert(
                "Error",
                "El nombre del curso es requerido.",
                "OK"
            );

            return false;
        }

        if (!int.TryParse(creditsText, out int credits) ||
            credits <= 0)
        {
            await DisplayAlert(
                "Error",
                "Los créditos deben ser un número mayor de 0.",
                "OK"
            );

            return false;
        }

        return true;
    }

    // Create manual course
    private async Task CreateManualCourseAsync(
        string code,
        string name,
        int credits)
    {
        var course = new Course
        {
            Codigo = code,
            Nombre = name,
            Creditos = credits,
            IsCompleted = false
        };

        await _dbService.SaveCourseAsync(course);
    }

    // Update existing course
    private async Task UpdateCourseAsync(
        string code,
        string name,
        int credits)
    {
        if (_courseBeingEdited == null)
            return;

        _courseBeingEdited.Codigo = code;
        _courseBeingEdited.Nombre = name;
        _courseBeingEdited.Creditos = credits;

        await _dbService.SaveCourseAsync(_courseBeingEdited);
    }

    // Delete selected course
    private async void OnDeleteCourseFromEditClicked(
        object sender,
        EventArgs e)
    {
        if (_isCreatingNewCourse ||
            _courseBeingEdited == null)
        {
            return;
        }

        bool confirm = await DisplayAlert(
            "Borrar curso",
            $"¿Seguro que deseas borrar {_courseBeingEdited.Nombre} ({_courseBeingEdited.Codigo})?",
            "Sí, borrar",
            "Cancelar"
        );

        if (!confirm)
            return;

        // Remove grade if course had one
        await _dbService.DeleteGradeForCourseAsync(
            _courseBeingEdited
        );

        await _dbService.DeleteCourseAsync(
            _courseBeingEdited
        );

        CloseCourseOverlay();

        await LoadDataAsync();

        await ShowToastAsync(
            "Curso eliminado correctamente."
        );
    }

    // Reset and hide overlay
    private void CloseCourseOverlay()
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

    // Show save progress button when checkbox changes
    private void OnCourseCheckedChanged(
        object sender,
        CheckedChangedEventArgs e)
    {
        if (_isLoadingData)
            return;

        SaveProgressButton.IsVisible = true;
    }

    // Simple toast animation
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