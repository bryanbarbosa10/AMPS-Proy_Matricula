using SQLite;

namespace AMPS;

public class Course
{
    // Local database id
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int StudentDbId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public int Creditos { get; set; }

    public bool IsCompleted { get; set; }
}