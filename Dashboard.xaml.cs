namespace AMPS;

public partial class Dashboard : ContentPage
{
    private readonly DataBaseServices _dbService;

    public Dashboard(DataBaseServices dbService)
    {
        InitializeComponent();
        _dbService = dbService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        if (!ActiveProfileService.HasActiveProfile)
        {
            var savedStudentId = await ActiveProfileService.GetSavedActiveStudentIdAsync();

            if (!string.IsNullOrWhiteSpace(savedStudentId))
            {
                var student = await _dbService.GetStudentByStudentIdAsync(savedStudentId);

                if (student != null)
                {
                    await ActiveProfileService.SetActiveStudentAsync(student);
                }
            }
        }

        if (!ActiveProfileService.HasActiveProfile)
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

            return;
        }

        var activeStudent = ActiveProfileService.CurrentStudent;

        if (activeStudent == null)
            return;

        WelcomeLabel.Text = $"Welcome, {activeStudent.Name}";

        if (!string.IsNullOrWhiteSpace(activeStudent.Nickname))
        {
            NicknameLabel.Text = activeStudent.Nickname;
            NicknameLabel.IsVisible = true;
        }
        else
        {
            NicknameLabel.Text = string.Empty;
            NicknameLabel.IsVisible = false;
        }

        StudentIdLabel.Text = activeStudent.StudentId;

        if (!string.IsNullOrWhiteSpace(activeStudent.Email))
        {
            StudentEmailLabel.Text = activeStudent.Email;
        }
        else
        {
            StudentEmailLabel.Text = "No registrado";
        }

        var courses = await _dbService.GetCoursesForActiveStudentAsync();

        int totalCourses = courses.Count;
        int completedCourses = courses.Count(c => c.IsCompleted);

        int totalCredits = courses.Sum(c => c.Creditos);
        int completedCredits = courses
            .Where(c => c.IsCompleted)
            .Sum(c => c.Creditos);

        CoursesDetectedLabel.Text = $"{totalCourses} detectados";
        CoursesCompletedLabel.Text = $"{completedCourses} completados";
        CreditsProgressLabel.Text = $"{completedCredits} / {totalCredits} créditos";

        CompletionDetailLabel.Text = $"{completedCourses} de {totalCourses} cursos";

        double completionPercent = 0;

        if (totalCourses > 0)
        {
            completionPercent = (double)completedCourses / totalCourses;
        }

        CompletionProgressBar.Progress = completionPercent;
        CompletionPercentLabel.Text = $"{completionPercent * 100:F0}%";

        double gpa = await _dbService.CalculateCurrentGpaForActiveStudentAsync();
        CurrentGpaLabel.Text = gpa.ToString("F2");

        CompletedCreditsLabel.Text = $"Créditos completados: {completedCredits}";
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