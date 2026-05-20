using SQLite;

namespace AMPS
{
    public class DataBaseServices
    {
        // Main SQLite connection
        private readonly SQLiteAsyncConnection _database;

        public DataBaseServices(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            _database.CreateTableAsync<Student>().Wait();
            _database.CreateTableAsync<Course>().Wait();
            _database.CreateTableAsync<Grade>().Wait();
            _database.CreateTableAsync<MatriculaItem>().Wait();
            _database.CreateTableAsync<MatriculaFile>().Wait();
            _database.CreateTableAsync<GpaHistory>().Wait();
        }

        // STUDENTS--------------------------------------------------------------------

        public async Task<bool> HasStudentsAsync()
        {
            int count = await _database.Table<Student>().CountAsync();

            return count > 0;
        }

        public Task<int> SaveStudentAsync(Student student)
        {
            return _database.InsertAsync(student);
        }

        public Task<List<Student>> GetStudentsAsync()
        {
            return _database.Table<Student>().ToListAsync();
        }

        public async Task<Student?> GetStudentByStudentIdAsync(string studentId)
        {
            return await _database.Table<Student>()
                .Where(student => student.StudentId == studentId)
                .FirstOrDefaultAsync();
        }

        public Task<int> UpdateStudentAsync(Student student)
        {
            return _database.UpdateAsync(student);
        }

        // COURSES--------------------------------------------------------------------

        public async Task<List<Course>> GetCoursesForActiveStudentAsync()
        {
            Student? activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return new List<Course>();

            return await _database.Table<Course>()
                .Where(course => course.StudentDbId == activeStudent.Id)
                .ToListAsync();
        }

        public async Task<int> SaveCourseAsync(Course course)
        {
            Student? activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                throw new Exception("No active profile found.");

            course.StudentDbId = activeStudent.Id;

            if (course.Id != 0)
                return await _database.UpdateAsync(course);

            return await _database.InsertAsync(course);
        }

        public Task<int> DeleteCourseAsync(Course course)
        {
            return _database.DeleteAsync(course);
        }

        // GRADES--------------------------------------------------------------------

        public async Task<List<Grade>> GetGradesForActiveStudentAsync()
        {
            Student? activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return new List<Grade>();

            return await _database.Table<Grade>()
                .Where(grade => grade.StudentDbId == activeStudent.Id)
                .ToListAsync();
        }

        public async Task<int> SaveGradeAsync(Grade grade)
        {
            Student? activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                throw new Exception("No active profile found.");

            grade.StudentDbId = activeStudent.Id;

            if (grade.Id != 0)
                return await _database.UpdateAsync(grade);

            return await _database.InsertAsync(grade);
        }

        public async Task<int> DeleteGradeForCourseAsync(Course course)
        {
            Student? activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return 0;

            List<Grade> existingGrades = await _database.Table<Grade>()
                .Where(grade =>
                    grade.StudentDbId == activeStudent.Id &&
                    grade.Materia == course.Nombre &&
                    grade.Creditos == course.Creditos)
                .ToListAsync();

            int deletedCount = 0;

            foreach (Grade grade in existingGrades)
            {
                deletedCount += await _database.DeleteAsync(grade);
            }

            return deletedCount;
        }

        public async Task<double> CalculateCurrentGpaForActiveStudentAsync()
        {
            List<Grade> grades = await GetGradesForActiveStudentAsync();

            if (grades.Count == 0)
                return 0.0;

            double honorPoints = grades.Sum(
                grade => grade.PuntosDeHonor * grade.Creditos
            );

            int totalCredits = grades.Sum(
                grade => grade.Creditos
            );

            if (totalCredits == 0)
                return 0.0;

            return honorPoints / totalCredits;
        }

        // GPA HISTORY--------------------------------------------------------------------

        public async Task<int> SaveGpaHistoryAsync(GpaHistory history)
        {
            Student? activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                throw new Exception("No active profile found.");

            history.StudentDbId = activeStudent.Id;

            return await _database.InsertAsync(history);
        }

        public async Task<List<GpaHistory>> GetGpaHistoryForActiveStudentAsync()
        {
            Student? activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return new List<GpaHistory>();

            List<GpaHistory> history = await _database.Table<GpaHistory>()
                .Where(item => item.StudentDbId == activeStudent.Id)
                .OrderByDescending(item => item.DateSaved)
                .ToListAsync();

            return history.Take(2).ToList();
        }

        // MATRICULAS--------------------------------------------------------------------

        public async Task<List<MatriculaItem>> GetMatriculasForActiveStudentAsync()
        {
            Student? activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return new List<MatriculaItem>();

            return await _database.Table<MatriculaItem>()
                .Where(matricula => matricula.StudentDbId == activeStudent.Id)
                .ToListAsync();
        }

        public async Task<int> SaveMatriculaAsync(MatriculaItem matricula)
        {
            Student? activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                throw new Exception("No active profile found.");

            matricula.StudentDbId = activeStudent.Id;

            if (matricula.Id != 0)
                return await _database.UpdateAsync(matricula);

            await _database.InsertAsync(matricula);

            return matricula.Id;
        }

        public Task<int> SaveMatriculaFileAsync(MatriculaFile file)
        {
            return _database.InsertAsync(file);
        }

        public async Task<List<MatriculaFile>> GetFilesForMatriculaAsync(int matriculaItemId)
        {
            return await _database.Table<MatriculaFile>()
                .Where(file => file.MatriculaItemId == matriculaItemId)
                .ToListAsync();
        }

        public async Task<int> DeleteMatriculaWithFilesAsync(MatriculaItem matricula)
        {
            List<MatriculaFile> files = await GetFilesForMatriculaAsync(matricula.Id);

            int deletedCount = 0;

            foreach (MatriculaFile file in files)
            {
                DeleteStoredFileIfExists(file.FilePath);

                deletedCount += await _database.DeleteAsync(file);
            }

            deletedCount += await _database.DeleteAsync(matricula);

            return deletedCount;
        }

        // RESET OPTIONS--------------------------------------------------------------------

        public async Task<int> ResetSecuencialForActiveStudentAsync()
        {
            Student? activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return 0;

            int affectedCount = 0;

            affectedCount += await DeleteCoursesForStudentAsync(activeStudent.Id);
            affectedCount += await DeleteGradesForStudentAsync(activeStudent.Id);
            affectedCount += await DeleteGpaHistoryForStudentAsync(activeStudent.Id);

            return affectedCount;
        }

        public async Task<int> ResetPromedioForActiveStudentAsync()
        {
            Student? activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return 0;

            int affectedCount = 0;

            affectedCount += await DeleteGradesForStudentAsync(activeStudent.Id);
            affectedCount += await DeleteGpaHistoryForStudentAsync(activeStudent.Id);
            affectedCount += await UncheckCompletedCoursesForStudentAsync(activeStudent.Id);

            return affectedCount;
        }

        public async Task<int> ResetMatriculaForActiveStudentAsync()
        {
            Student? activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return 0;

            List<MatriculaItem> matriculas = await _database.Table<MatriculaItem>()
                .Where(matricula => matricula.StudentDbId == activeStudent.Id)
                .ToListAsync();

            int deletedCount = 0;

            foreach (MatriculaItem matricula in matriculas)
            {
                deletedCount += await DeleteMatriculaWithFilesAsync(matricula);
            }

            return deletedCount;
        }

        // HELPERS--------------------------------------------------------------------

        private async Task<int> DeleteCoursesForStudentAsync(int studentDbId)
        {
            List<Course> courses = await _database.Table<Course>()
                .Where(course => course.StudentDbId == studentDbId)
                .ToListAsync();

            int deletedCount = 0;

            foreach (Course course in courses)
            {
                deletedCount += await _database.DeleteAsync(course);
            }

            return deletedCount;
        }

        private async Task<int> DeleteGradesForStudentAsync(int studentDbId)
        {
            List<Grade> grades = await _database.Table<Grade>()
                .Where(grade => grade.StudentDbId == studentDbId)
                .ToListAsync();

            int deletedCount = 0;

            foreach (Grade grade in grades)
            {
                deletedCount += await _database.DeleteAsync(grade);
            }

            return deletedCount;
        }

        private async Task<int> DeleteGpaHistoryForStudentAsync(int studentDbId)
        {
            List<GpaHistory> historyItems = await _database.Table<GpaHistory>()
                .Where(history => history.StudentDbId == studentDbId)
                .ToListAsync();

            int deletedCount = 0;

            foreach (GpaHistory history in historyItems)
            {
                deletedCount += await _database.DeleteAsync(history);
            }

            return deletedCount;
        }

        private async Task<int> UncheckCompletedCoursesForStudentAsync(int studentDbId)
        {
            List<Course> courses = await _database.Table<Course>()
                .Where(course => course.StudentDbId == studentDbId)
                .ToListAsync();

            int affectedCount = 0;

            foreach (Course course in courses)
            {
                if (!course.IsCompleted)
                    continue;

                course.IsCompleted = false;

                affectedCount += await _database.UpdateAsync(course);
            }

            return affectedCount;
        }

        private void DeleteStoredFileIfExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}