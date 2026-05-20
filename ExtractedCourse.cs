namespace AMPS;

public class ExtractedCourse
{
    // Temporary course data extracted from a document
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public int Creditos { get; set; }

    public bool IsSelected { get; set; } = true;
}