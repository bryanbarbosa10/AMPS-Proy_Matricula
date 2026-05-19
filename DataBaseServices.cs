


using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMPS
{
    public class DataBaseServices
    {
        // Main connection to db
        private readonly SQLiteAsyncConnection _database;

        public DataBaseServices(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);

            // Create tables if not already
            _database.CreateTableAsync<Student>().Wait();
            _database.CreateTableAsync<Course>().Wait();
            _database.CreateTableAsync<Grade>().Wait();
            _database.CreateTableAsync<MatriculaItem>().Wait();
            _database.CreateTableAsync<MatriculaFile>().Wait();
            _database.CreateTableAsync<GpaHistory>().Wait();
        }



        // STUDENTS-----------------------------------------

        // Check for profiles
        public async Task<bool> HasStudentsAsync()
        {
            var count = await _database.Table<Student>().CountAsync();
            return count > 0;
        }

        // New Student profiles
        public Task<int> SaveStudentAsync(Student student)
        {
            return _database.InsertAsync(student);
        }

        // All profiles available
        public Task<List<Student>> GetStudentsAsync()
        {
            return _database.Table<Student>().ToListAsync();
        }

        // Get student by database Id
        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            return await _database.Table<Student>()
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();
        }

        // Validate unique student id
        public async Task<Student?> GetStudentByStudentIdAsync(string studentId)
        {
            return await _database.Table<Student>()
                .Where(s => s.StudentId == studentId)
                .FirstOrDefaultAsync();
        }

        // Update an existing student profile
        public Task<int> UpdateStudentAsync(Student student)
        {
            return _database.UpdateAsync(student);
        }



        // COURSES-----------------------------------------

        // Shows all courses saved
        public Task<List<Course>> GetCoursesAsync()
        {
            return _database.Table<Course>().ToListAsync();
        }

        // Shows courses only for active profile
        public async Task<List<Course>> GetCoursesForActiveStudentAsync()
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return new List<Course>();

            int studentDbId = activeStudent.Id;

            return await _database.Table<Course>()
                .Where(c => c.StudentDbId == studentDbId)
                .ToListAsync();
        }

        // Add or edit course for active profile
        public async Task<int> SaveCourseAsync(Course course)
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                throw new Exception("No hay perfil activo.");

            course.StudentDbId = activeStudent.Id;

            if (course.Id != 0)
                return await _database.UpdateAsync(course);

            return await _database.InsertAsync(course);
        }

        public async Task<int> DeleteCourseAsync(Course course)
        {
            return await _database.DeleteAsync(course);
        }



        // GRADES------------------------------------------

        // Show all grades
        public Task<List<Grade>> GetGradesAsync()
        {
            return _database.Table<Grade>().ToListAsync();
        }

        // Shows grades only for active profile
        public async Task<List<Grade>> GetGradesForActiveStudentAsync()
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return new List<Grade>();

            int studentDbId = activeStudent.Id;

            return await _database.Table<Grade>()
                .Where(g => g.StudentDbId == studentDbId)
                .ToListAsync();
        }

        // Save grade for active profile
        public async Task<int> SaveGradeAsync(Grade grade)
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                throw new Exception("No hay perfil activo.");

            grade.StudentDbId = activeStudent.Id;

            if (grade.Id != 0)
                return await _database.UpdateAsync(grade);

            return await _database.InsertAsync(grade);
        }

        // Delete grades only for active profile
        public async Task<int> ClearGradesForActiveStudentAsync()
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return 0;

            int studentDbId = activeStudent.Id;

            var grades = await _database.Table<Grade>()
                .Where(g => g.StudentDbId == studentDbId)
                .ToListAsync();

            int deletedCount = 0;

            foreach (var grade in grades)
            {
                deletedCount += await _database.DeleteAsync(grade);
            }

            return deletedCount;
        }

        // Delete all grades from database
        public Task<int> ClearGradesAsync()
        {
            return _database.DeleteAllAsync<Grade>();
        }

        public async Task<int> DeleteGradeForCourseAsync(Course course)
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return 0;

            var existingGrades = await _database.Table<Grade>()
                .Where(g => g.StudentDbId == activeStudent.Id &&
                            g.Materia == course.Nombre &&
                            g.Creditos == course.Creditos)
                .ToListAsync();

            int deletedCount = 0;

            foreach (var grade in existingGrades)
            {
                deletedCount += await _database.DeleteAsync(grade);
            }

            return deletedCount;
        }

        public async Task<double> CalculateCurrentGpaForActiveStudentAsync()
        {
            var grades = await GetGradesForActiveStudentAsync();

            if (grades.Count == 0)
                return 0.0;

            double honorPoints = grades.Sum(g => g.PuntosDeHonor * g.Creditos);
            int totalCredits = grades.Sum(g => g.Creditos);

            if (totalCredits == 0)
                return 0.0;

            return honorPoints / totalCredits;
        }

        public async Task<int> SaveGpaHistoryAsync(GpaHistory history)
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                throw new Exception("No hay perfil activo.");

            history.StudentDbId = activeStudent.Id;

            return await _database.InsertAsync(history);
        }

        public async Task<List<GpaHistory>> GetGpaHistoryForActiveStudentAsync()
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return new List<GpaHistory>();

            var history = await _database.Table<GpaHistory>()
                .Where(h => h.StudentDbId == activeStudent.Id)
                .OrderByDescending(h => h.DateSaved)
                .ToListAsync();

            return history.Take(2).ToList();
        }

        // MATRICULAS------------------------------------------

        public async Task<List<MatriculaItem>> GetMatriculasForActiveStudentAsync()
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return new List<MatriculaItem>();

            int studentDbId = activeStudent.Id;

            return await _database.Table<MatriculaItem>()
                .Where(m => m.StudentDbId == studentDbId)
                .ToListAsync();
        }

        public async Task<int> SaveMatriculaAsync(MatriculaItem matricula)
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                throw new Exception("No hay perfil activo.");

            matricula.StudentDbId = activeStudent.Id;

            if (matricula.Id != 0)
                return await _database.UpdateAsync(matricula);

            await _database.InsertAsync(matricula);

            return matricula.Id;
        }

        public async Task<int> DeleteMatriculaAsync(MatriculaItem matricula)
        {
            return await _database.DeleteAsync(matricula);
        }
        public async Task<int> SaveMatriculaFileAsync(MatriculaFile file)
        {
            return await _database.InsertAsync(file);
        }

        public async Task<List<MatriculaFile>> GetFilesForMatriculaAsync(int matriculaItemId)
        {
            return await _database.Table<MatriculaFile>()
                .Where(f => f.MatriculaItemId == matriculaItemId)
                .ToListAsync();
        }

        public async Task<int> DeleteMatriculaFileAsync(MatriculaFile file)
        {
            return await _database.DeleteAsync(file);
        }
        public async Task<int> DeleteMatriculaWithFilesAsync(MatriculaItem matricula)
        {
            var files = await GetFilesForMatriculaAsync(matricula.Id);

            foreach (var file in files)
            {
                if (!string.IsNullOrWhiteSpace(file.FilePath) && File.Exists(file.FilePath))
                {
                    File.Delete(file.FilePath);
                }

                await _database.DeleteAsync(file);
            }

            return await _database.DeleteAsync(matricula);
        }

        //Settings

        public async Task<int> ResetSecuencialForActiveStudentAsync()
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return 0;

            int deletedCount = 0;

            var courses = await _database.Table<Course>()
                .Where(c => c.StudentDbId == activeStudent.Id)
                .ToListAsync();

            foreach (var course in courses)
            {
                deletedCount += await _database.DeleteAsync(course);
            }

            var grades = await _database.Table<Grade>()
                .Where(g => g.StudentDbId == activeStudent.Id)
                .ToListAsync();

            foreach (var grade in grades)
            {
                deletedCount += await _database.DeleteAsync(grade);
            }

            var histories = await _database.Table<GpaHistory>()
                .Where(h => h.StudentDbId == activeStudent.Id)
                .ToListAsync();

            foreach (var history in histories)
            {
                deletedCount += await _database.DeleteAsync(history);
            }

            return deletedCount;
        }

        public async Task<int> ResetPromedioForActiveStudentAsync()
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return 0;

            int affectedCount = 0;

            var grades = await _database.Table<Grade>()
                .Where(g => g.StudentDbId == activeStudent.Id)
                .ToListAsync();

            foreach (var grade in grades)
            {
                affectedCount += await _database.DeleteAsync(grade);
            }

            var histories = await _database.Table<GpaHistory>()
                .Where(h => h.StudentDbId == activeStudent.Id)
                .ToListAsync();

            foreach (var history in histories)
            {
                affectedCount += await _database.DeleteAsync(history);
            }

            var courses = await _database.Table<Course>()
                .Where(c => c.StudentDbId == activeStudent.Id)
                .ToListAsync();

            foreach (var course in courses)
            {
                if (course.IsCompleted)
                {
                    course.IsCompleted = false;
                    affectedCount += await _database.UpdateAsync(course);
                }
            }

            return affectedCount;
        }

        public async Task<int> ResetMatriculaForActiveStudentAsync()
        {
            var activeStudent = ActiveProfileService.CurrentStudent;

            if (activeStudent == null)
                return 0;

            int deletedCount = 0;

            var matriculas = await _database.Table<MatriculaItem>()
                .Where(m => m.StudentDbId == activeStudent.Id)
                .ToListAsync();

            foreach (var matricula in matriculas)
            {
                var files = await GetFilesForMatriculaAsync(matricula.Id);

                foreach (var file in files)
                {
                    if (!string.IsNullOrWhiteSpace(file.FilePath) && File.Exists(file.FilePath))
                    {
                        File.Delete(file.FilePath);
                    }

                    deletedCount += await _database.DeleteAsync(file);
                }

                deletedCount += await _database.DeleteAsync(matricula);
            }

            return deletedCount;
        }
    }
}