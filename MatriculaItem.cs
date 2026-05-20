using SQLite;

namespace AMPS;

public class MatriculaItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int StudentDbId { get; set; }

    public string Semestre { get; set; } = string.Empty;

    public string Year { get; set; } = string.Empty;

    public string FileMode { get; set; } = string.Empty;

    // Date when the enrollment record was saved
    public DateTime DateSaved { get; set; }
}