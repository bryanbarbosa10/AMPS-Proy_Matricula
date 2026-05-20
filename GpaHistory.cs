using SQLite;

namespace AMPS;

public class GpaHistory
{
    // Local database id
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int StudentDbId { get; set; }

    public double PreviousGpa { get; set; }

    public double NewGpa { get; set; }

    // Summary of courses added during GPA update
    public string AddedCoursesSummary { get; set; } = string.Empty;

    public int TotalCreditsAdded { get; set; }

    public DateTime DateSaved { get; set; }
}