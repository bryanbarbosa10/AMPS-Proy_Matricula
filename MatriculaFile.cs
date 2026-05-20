using SQLite;

namespace AMPS;

public class MatriculaFile
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int MatriculaItemId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    // Date when the file was saved internally
    public DateTime DateSaved { get; set; }
}