using SQLite;

namespace AMPS;

public class GpaHistory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int StudentDbId { get; set; }

    public double PreviousGpa { get; set; }

    public double NewGpa { get; set; }

    public string AddedCourseName { get; set; } = string.Empty;

    public string AddedCourseCode { get; set; } = string.Empty;

    public int Credits { get; set; }

    public string GradeLetter { get; set; } = string.Empty;

    public DateTime DateSaved { get; set; }
}