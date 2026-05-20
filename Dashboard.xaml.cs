namespace AMPS;

public partial class Dashboard : ContentPage
{
    // Main database service
    private readonly DataBaseServices _dbService;

    public Dashboard(DataBaseServices dbService)
    {
        InitializeComponent();

        _dbService = dbService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Reload dashboard every time page appears
        await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        await EnsureActiveProfileAsync();

        // If no profile exists, show empty dashboard
        if (!ActiveProfileService.HasActiveProfile)
        {
            ShowEmptyDashboard();
            return;
        }

        Student? activeStudent = ActiveProfileService.CurrentStudent;

        if (activeStudent == null)
            return;

        await LoadStudentInfoAsync(activeStudent);

        // Load GPA, credits and course progress
        await LoadAcademicProgressAsync();
    }

    private async Task EnsureActiveProfileAsync()
    {
        if (ActiveProfileService.HasActiveProfile)
            return;

        string? savedStudentId = await ActiveProfileService.GetSavedActiveStudentIdAsync();

        if (string.IsNullOrWhiteSpace(savedStudentId))
            return;

        Student? student = await _dbService.GetStudentByStudentIdAsync(savedStudentId);

        if (student != null)
        {
            await ActiveProfileService.SetActiveStudentAsync(student);
        }
    }

    private void ShowEmptyDashboard()
    {
        WelcomeLabel.Text = "No hay perfil activo";
        NicknameLabel.IsVisible = false;

        StudentIdLabel.Text = "N/A";
        StudentEmailLabel.Text = "No registrado";

        CurrentGpaLabel.Text = "0.00";
        CompletedCreditsLabel.Text = "Créditos completados: 0";

        CoursesDetectedLabel.Text = "0 detectados";
        CoursesCompletedLabel.Text = "0 completados";
        CreditsProgressLabel.Text = "0 / 0 créditos";

        CompletionPercentLabel.Text = "0%";
        CompletionProgressBar.Progress = 0;

        CompletionDetailLabel.Text = "0 de 0 cursos";
    }

    // Load active student basic information
    private Task LoadStudentInfoAsync(Student student)
    {
        WelcomeLabel.Text = $"Bienvenido, {student.Name}";

        if (!string.IsNullOrWhiteSpace(student.Nickname))
        {
            NicknameLabel.Text = student.Nickname;
            NicknameLabel.IsVisible = true;
        }
        else
        {
            NicknameLabel.Text = string.Empty;
            NicknameLabel.IsVisible = false;
        }

        StudentIdLabel.Text = student.StudentId;

        StudentEmailLabel.Text = !string.IsNullOrWhiteSpace(student.Email)
            ? student.Email
            : "No registrado";

        return Task.CompletedTask;
    }

    private async Task LoadAcademicProgressAsync()
    {
        var courses = await _dbService.GetCoursesForActiveStudentAsync();

        int totalCourses = courses.Count;

        int completedCourses = courses.Count(
            course => course.IsCompleted
        );

        int totalCredits = courses.Sum(
            course => course.Creditos
        );

        int completedCredits = courses
            .Where(course => course.IsCompleted)
            .Sum(course => course.Creditos);

        CoursesDetectedLabel.Text = $"{totalCourses} detectados";

        CoursesCompletedLabel.Text = $"{completedCourses} completados";

        CreditsProgressLabel.Text =
            $"{completedCredits} / {totalCredits} créditos";

        CompletionDetailLabel.Text =
            $"{completedCourses} de {totalCourses} cursos";

        // Calculate completion percentage
        double completionPercent = totalCourses > 0
            ? (double)completedCourses / totalCourses
            : 0;

        CompletionProgressBar.Progress = completionPercent;

        CompletionPercentLabel.Text =
            $"{completionPercent * 100:F0}%";

        double currentGpa =
            await _dbService.CalculateCurrentGpaForActiveStudentAsync();

        CurrentGpaLabel.Text = currentGpa.ToString("F2");

        CompletedCreditsLabel.Text =
            $"Créditos completados: {completedCredits}";
    }

    private async void OnSecuencialClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Secuencial");
    }

    private async void OnPromedioClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Promedio");
    }

    private async void OnMatriculaClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Matricula");
    }

    private async void OnProfilesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ProfileManagement");
    }
}