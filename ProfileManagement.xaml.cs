using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace AMPS;

public partial class ProfileManagement : ContentPage
{
    // Main database service
    private readonly DataBaseServices _dbService;

    private Student? _studentBeingEdited;

    public ObservableCollection<Student> Profiles { get; set; } = new();

    public ProfileManagement(DataBaseServices dbService)
    {
        InitializeComponent();

        _dbService = dbService;

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadProfilesAsync();

        UpdateActiveProfileLabel();
    }

    private async Task LoadProfilesAsync()
    {
        Profiles.Clear();

        List<Student> students = await _dbService.GetStudentsAsync();

        foreach (Student student in students)
        {
            Profiles.Add(student);
        }

        ProfilesCollectionView.ItemsSource = Profiles;
    }

    private void UpdateActiveProfileLabel()
    {
        Student? activeStudent = ActiveProfileService.CurrentStudent;

        if (activeStudent == null)
        {
            ActiveProfileLabel.Text = "Perfil activo: Ninguno";
            return;
        }

        if (!string.IsNullOrWhiteSpace(activeStudent.Nickname))
        {
            ActiveProfileLabel.Text =
                $"Perfil activo: {activeStudent.Nickname}";
        }
        else
        {
            ActiveProfileLabel.Text =
                $"Perfil activo: {activeStudent.Name}";
        }
    }

    private async void OnAddProfileClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(ProfileCreation)}?from=profiles"
        );
    }

    private async void OnSelectProfileClicked(object sender, EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter is not Student selectedStudent)
        {
            return;
        }

        await ActiveProfileService
            .SetActiveStudentAsync(selectedStudent);

        UpdateActiveProfileLabel();

        string displayName =
            !string.IsNullOrWhiteSpace(selectedStudent.Nickname)
            ? selectedStudent.Nickname
            : selectedStudent.Name;

        await ShowToastAsync(
            $"Perfil activo: {displayName}"
        );
    }

    private void OnEditProfileClicked(object sender, EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter is not Student student)
        {
            return;
        }

        _studentBeingEdited = student;

        LoadEditOverlay(student);

        EditProfileOverlay.IsVisible = true;
    }

    private void LoadEditOverlay(Student student)
    {
        EditStudentIdEntry.Text = student.StudentId;
        EditNameEntry.Text = student.Name;
        EditNicknameEntry.Text = student.Nickname;
        EditEmailEntry.Text = student.Email;
    }

    private void OnCancelEditProfileClicked(object sender, EventArgs e)
    {
        ClearEditOverlay();

        EditProfileOverlay.IsVisible = false;
    }

    private async void OnSaveEditProfileClicked(object sender, EventArgs e)
    {
        if (_studentBeingEdited == null)
            return;

        string name = EditNameEntry.Text?.Trim() ?? string.Empty;

        string nickname = EditNicknameEntry.Text?.Trim() ?? string.Empty;

        string email = EditEmailEntry.Text?.Trim() ?? string.Empty;

        bool isValid = await ValidateProfileEditAsync(
            name,
            nickname,
            email
        );

        if (!isValid)
            return;

        _studentBeingEdited.Name = name;
        _studentBeingEdited.Nickname = nickname;
        _studentBeingEdited.Email = email;

        await _dbService.UpdateStudentAsync(_studentBeingEdited);

        if (ActiveProfileService.HasActiveProfile &&
            ActiveProfileService.CurrentStudent?.Id ==
            _studentBeingEdited.Id)
        {
            await ActiveProfileService
                .SetActiveStudentAsync(_studentBeingEdited);
        }

        ClearEditOverlay();

        EditProfileOverlay.IsVisible = false;

        await LoadProfilesAsync();

        UpdateActiveProfileLabel();

        await ShowToastAsync(
            "Perfil actualizado correctamente."
        );
    }

    private async Task<bool> ValidateProfileEditAsync(
        string name,
        string nickname,
        string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert(
                "Error",
                "El nombre es requerido.",
                "OK"
            );

            return false;
        }

        if (name.Length > 30)
        {
            await DisplayAlert(
                "Error",
                "El nombre no puede exceder 30 caracteres.",
                "OK"
            );

            return false;
        }

        if (nickname.Length > 40)
        {
            await DisplayAlert(
                "Error",
                "El nickname no puede exceder 40 caracteres.",
                "OK"
            );

            return false;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            bool validEmail = Regex.IsMatch(
                email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
            );

            if (!validEmail)
            {
                await DisplayAlert(
                    "Error",
                    "Escribe un email válido.",
                    "OK"
                );

                return false;
            }
        }

        return true;
    }

    private void ClearEditOverlay()
    {
        _studentBeingEdited = null;

        EditStudentIdEntry.Text = string.Empty;
        EditNameEntry.Text = string.Empty;
        EditNicknameEntry.Text = string.Empty;
        EditEmailEntry.Text = string.Empty;
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