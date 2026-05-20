using SQLite;

namespace AMPS
{
    public class Student
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Unique student number
        [Unique]
        public string StudentId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Nickname { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}