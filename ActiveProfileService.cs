namespace AMPS
{
    public static class ActiveProfileService
    {
        // Key used to store active student securely
        private const string ActiveStudentIdKey = "ActiveStudentId";

        // Current active profile in memory
        public static Student? CurrentStudent { get; private set; }

        public static bool HasActiveProfile => CurrentStudent != null;

        // Set active profile and save it securely
        public static async Task SetActiveStudentAsync(Student student)
        {
            CurrentStudent = student;

            await SecureStorage.SetAsync(
                ActiveStudentIdKey,
                student.StudentId
            );
        }

        // Retrieve previously saved active student id
        public static async Task<string?> GetSavedActiveStudentIdAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(ActiveStudentIdKey);
            }
            catch
            {
                return null;
            }
        }

        // Remove active profile from memory and storage
        public static void ClearActiveStudent()
        {
            CurrentStudent = null;

            SecureStorage.Remove(ActiveStudentIdKey);
        }
    }
}