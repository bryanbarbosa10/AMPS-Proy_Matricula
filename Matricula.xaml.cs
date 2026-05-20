using System.Collections.ObjectModel;

namespace AMPS;

public partial class Matricula : ContentPage
{
    // Main database service
    private readonly DataBaseServices _dbService;

    // Selected file for PDF or Word mode
    private FileResult? _selectedSingleFile;

    // Selected files for photo mode
    private List<FileResult> _selectedPhotoFiles = new();

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

        // Prevent using matricula without active profile
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

    // Load saved enrollment records
    private async Task LoadMatriculasAsync()
    {
        Matriculas.Clear();

        List<MatriculaItem> matriculas =
            await _dbService.GetMatriculasForActiveStudentAsync();

        foreach (MatriculaItem matricula in matriculas)
        {
            Matriculas.Add(matricula);
        }

        MatriculasCollectionView.ItemsSource = Matriculas;
    }

    private void OnAddMatriculaClicked(object sender, EventArgs e)
    {
        ResetOverlayFields();

        AddMatriculaOverlay.IsVisible = true;
    }

    private void OnFileModeChanged(object sender, EventArgs e)
    {
        string mode = GetSelectedFileMode();

        _selectedSingleFile = null;
        _selectedPhotoFiles.Clear();

        SelectedFileLabel.Text = "Ningún archivo seleccionado";

        PickFileButton.Text = mode == "PHOTOS"
            ? "Seleccionar fotos"
            : "Seleccionar archivo";
    }

    private async void OnPickMatriculaFileClicked(object sender, EventArgs e)
    {
        string mode = GetSelectedFileMode();

        if (string.IsNullOrWhiteSpace(mode))
        {
            await DisplayAlert(
                "Error",
                "Primero selecciona el tipo de evidencia.",
                "OK"
            );

            return;
        }

        try
        {
            if (mode == "PDF")
            {
                await PickSingleFileAsync(".pdf");
            }
            else if (mode == "WORD")
            {
                await PickSingleFileAsync(".docx");
            }
            else if (mode == "PHOTOS")
            {
                await PickMultiplePhotosAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error",
                $"No se pudo seleccionar el archivo: {ex.Message}",
                "OK"
            );
        }
    }

    // Pick one PDF or Word file
    private async Task PickSingleFileAsync(string extension)
    {
        FilePickerFileType fileType;

        if (extension == ".pdf")
        {
            fileType = FilePickerFileType.Pdf;
        }
        else
        {
            fileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    {
                        DevicePlatform.iOS,
                        new[] { "org.openxmlformats.wordprocessingml.document" }
                    },

                    {
                        DevicePlatform.Android,
                        new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
                    },

                    {
                        DevicePlatform.WinUI,
                        new[] { ".docx" }
                    }
                });
        }

        FileResult? result =
            await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona archivo",
                FileTypes = fileType
            });

        if (result == null)
            return;

        _selectedSingleFile = result;

        _selectedPhotoFiles.Clear();

        SelectedFileLabel.Text = $"Archivo: {result.FileName}";
    }

    // Pick multiple image files
    private async Task PickMultiplePhotosAsync()
    {
        var imageFileType = new FilePickerFileType(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                {
                    DevicePlatform.iOS,
                    new[] { "public.image" }
                },

                {
                    DevicePlatform.Android,
                    new[] { "image/*" }
                },

                {
                    DevicePlatform.WinUI,
                    new[] { ".jpg", ".jpeg", ".png" }
                }
            });

        IEnumerable<FileResult>? results =
            await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Selecciona fotos de matrícula",
                FileTypes = imageFileType
            });

        if (results == null)
            return;

        _selectedPhotoFiles = results.ToList();

        _selectedSingleFile = null;

        SelectedFileLabel.Text =
            $"{_selectedPhotoFiles.Count} foto(s) seleccionada(s)";
    }

    private void OnCancelMatriculaClicked(object sender, EventArgs e)
    {
        ResetOverlayFields();

        AddMatriculaOverlay.IsVisible = false;
    }

    // Save new enrollment record
    private async void OnSaveMatriculaClicked(object sender, EventArgs e)
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

        string semesterName =
            SemesterEntry.Text?.Trim() ?? string.Empty;

        string fileMode = GetSelectedFileMode();

        if (!await ValidateMatriculaFormAsync(
            semesterName,
            fileMode))
        {
            return;
        }

        var matricula = new MatriculaItem
        {
            Semestre = semesterName,
            Year = DateTime.Now.Year.ToString(),
            FileMode = fileMode,
            DateSaved = DateTime.Now
        };

        int matriculaId =
            await _dbService.SaveMatriculaAsync(matricula);

        await SaveSelectedFilesAsync(fileMode, matriculaId);

        ResetOverlayFields();

        AddMatriculaOverlay.IsVisible = false;

        await LoadMatriculasAsync();

        await ShowToastAsync(
            "Matrícula guardada correctamente."
        );
    }

    // Validate semester and selected files
    private async Task<bool> ValidateMatriculaFormAsync(
        string semesterName,
        string fileMode)
    {
        if (string.IsNullOrWhiteSpace(semesterName))
        {
            await DisplayAlert(
                "Error",
                "Escribe el nombre del semestre.",
                "OK"
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(fileMode))
        {
            await DisplayAlert(
                "Error",
                "Selecciona el tipo de evidencia.",
                "OK"
            );

            return false;
        }

        if ((fileMode == "PDF" || fileMode == "WORD") &&
            _selectedSingleFile == null)
        {
            await DisplayAlert(
                "Error",
                "Selecciona un archivo.",
                "OK"
            );

            return false;
        }

        if (fileMode == "PHOTOS" &&
            _selectedPhotoFiles.Count == 0)
        {
            await DisplayAlert(
                "Error",
                "Selecciona una o más fotos.",
                "OK"
            );

            return false;
        }

        return true;
    }

    // Save selected files into internal app storage
    private async Task SaveSelectedFilesAsync(
        string fileMode,
        int matriculaId)
    {
        if ((fileMode == "PDF" || fileMode == "WORD") &&
            _selectedSingleFile != null)
        {
            MatriculaFile storedFile =
                await CopyFileToInternalStorageAsync(
                    _selectedSingleFile,
                    matriculaId
                );

            await _dbService.SaveMatriculaFileAsync(storedFile);

            return;
        }

        if (fileMode == "PHOTOS")
        {
            foreach (FileResult photo in _selectedPhotoFiles)
            {
                MatriculaFile storedFile =
                    await CopyFileToInternalStorageAsync(
                        photo,
                        matriculaId
                    );

                await _dbService.SaveMatriculaFileAsync(storedFile);
            }
        }
    }

    // Copy external file into app data directory
    private async Task<MatriculaFile> CopyFileToInternalStorageAsync(
        FileResult file,
        int matriculaId)
    {
        string folder = Path.Combine(
            FileSystem.AppDataDirectory,
            "matriculas",
            matriculaId.ToString()
        );

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string extension =
            Path.GetExtension(file.FileName).ToLower();

        string storedFileName =
            $"{Guid.NewGuid()}{extension}";

        string destinationPath =
            Path.Combine(folder, storedFileName);

        using var sourceStream =
            await file.OpenReadAsync();

        using var destinationStream =
            File.Create(destinationPath);

        await sourceStream.CopyToAsync(destinationStream);

        return new MatriculaFile
        {
            MatriculaItemId = matriculaId,
            FileName = file.FileName,
            StoredFileName = storedFileName,
            FilePath = destinationPath,
            FileType = extension,
            DateSaved = DateTime.Now
        };
    }

    private async void OnOpenMatriculaClicked(object sender, EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter is not MatriculaItem matricula)
        {
            return;
        }

        await OpenMatriculaAsync(matricula);
    }

    // Open saved enrollment evidence
    private async Task OpenMatriculaAsync(MatriculaItem matricula)
    {
        List<MatriculaFile> files =
            await _dbService.GetFilesForMatriculaAsync(matricula.Id);

        if (files.Count == 0)
        {
            await DisplayAlert(
                "Sin archivos",
                "Esta matrícula no tiene archivos guardados.",
                "OK"
            );

            return;
        }

        if (files.Count == 1)
        {
            await OpenStoredFileAsync(files[0]);

            return;
        }

        string selected = await DisplayActionSheet(
            "Fotos guardadas",
            "Cancelar",
            null,
            files.Select(file => file.FileName).ToArray()
        );

        if (selected == "Cancelar" ||
            string.IsNullOrWhiteSpace(selected))
        {
            return;
        }

        MatriculaFile? selectedFile =
            files.FirstOrDefault(file => file.FileName == selected);

        if (selectedFile != null)
        {
            await OpenStoredFileAsync(selectedFile);
        }
    }

    // Open file with system viewer
    private async Task OpenStoredFileAsync(MatriculaFile file)
    {
        if (string.IsNullOrWhiteSpace(file.FilePath) ||
            !File.Exists(file.FilePath))
        {
            await DisplayAlert(
                "Archivo no encontrado",
                "No se encontró el archivo guardado.",
                "OK"
            );

            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(
                new OpenFileRequest
                {
                    File = new ReadOnlyFile(file.FilePath)
                });
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error",
                $"No se pudo abrir el archivo: {ex.Message}",
                "OK"
            );
        }
    }

    private async void OnDeleteMatriculaClicked(object sender, EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter is not MatriculaItem matricula)
        {
            return;
        }

        string confirmText = await DisplayPromptAsync(
            "Confirmar eliminación",
            $"Para borrar esta matrícula, escribe exactamente:\n\n{matricula.Semestre}",
            "Borrar",
            "Cancelar",
            placeholder: matricula.Semestre
        );

        if (string.IsNullOrWhiteSpace(confirmText))
            return;

        if (confirmText.Trim() != matricula.Semestre.Trim())
        {
            await DisplayAlert(
                "Confirmación incorrecta",
                "El nombre escrito no coincide. No se borró la matrícula.",
                "OK"
            );

            return;
        }

        await _dbService.DeleteMatriculaWithFilesAsync(matricula);

        await LoadMatriculasAsync();

        await ShowToastAsync(
            "Matrícula eliminada correctamente."
        );
    }

    private string GetSelectedFileMode()
    {
        return FileModePicker.SelectedItem?.ToString() ?? string.Empty;
    }

    private void ResetOverlayFields()
    {
        _selectedSingleFile = null;

        _selectedPhotoFiles.Clear();

        SemesterEntry.Text = string.Empty;

        FileModePicker.SelectedItem = null;

        SelectedFileLabel.Text = "Ningún archivo seleccionado";

        PickFileButton.Text = "Seleccionar archivo";
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