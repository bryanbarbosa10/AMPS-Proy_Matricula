using DocumentFormat.OpenXml.Packaging;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace AMPS;

public class CourseExtractionService
{
    // Extract courses from PDF or Word document
    public async Task<List<ExtractedCourse>> ExtractCoursesAsync(FileResult file)
    {
        string extension = Path.GetExtension(file.FileName).ToLower();

        string extractedText = extension switch
        {
            ".pdf" => await ExtractTextFromPdfAsync(file),
            ".docx" => await ExtractTextFromWordAsync(file),
            _ => string.Empty
        };

        return ParseCoursesFromText(extractedText);
    }

    private async Task<string> ExtractTextFromPdfAsync(FileResult file)
    {
        string tempPath = await CopyFileToTempAsync(file);

        var builder = new StringBuilder();

        using var document = PdfDocument.Open(tempPath);

        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private async Task<string> ExtractTextFromWordAsync(FileResult file)
    {
        string tempPath = await CopyFileToTempAsync(file);

        var builder = new StringBuilder();

        using var wordDocument = WordprocessingDocument.Open(tempPath, false);

        var body = wordDocument.MainDocumentPart?.Document?.Body;

        if (body != null)
        {
            builder.AppendLine(body.InnerText);
        }

        return builder.ToString();
    }

    // Copy uploaded file into temporary cache
    private async Task<string> CopyFileToTempAsync(FileResult file)
    {
        string tempPath = Path.Combine(
            FileSystem.CacheDirectory,
            file.FileName
        );

        using var sourceStream = await file.OpenReadAsync();

        using var destinationStream = File.Create(tempPath);

        await sourceStream.CopyToAsync(destinationStream);

        return tempPath;
    }

    private List<ExtractedCourse> ParseCoursesFromText(string text)
    {
        var extractedCourses = new List<ExtractedCourse>();

        if (string.IsNullOrWhiteSpace(text))
            return extractedCourses;

        text = CleanExtractedText(text);

        string pattern =
            @"(?<code>\*?[A-Z]{4}\s?(?:\d{4}|_{2,}\d{0,2}))\s*[–—-]\s*(?<name>.*?)(?=\s+(?<credits>[1-5])(?:\s|$))";

        var matches = Regex.Matches(
            text,
            pattern,
            RegexOptions.Singleline
        );

        foreach (Match match in matches)
        {
            string code = match.Groups["code"].Value
                .Replace("*", "")
                .Trim();

            string name = match.Groups["name"].Value.Trim();

            string creditsText = match.Groups["credits"].Value.Trim();

            if (!int.TryParse(creditsText, out int credits))
                continue;

            name = CleanCourseName(name);

            if (string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            bool alreadyExists = extractedCourses.Any(course =>
                course.Codigo.Equals(code, StringComparison.OrdinalIgnoreCase) &&
                course.Nombre.Equals(name, StringComparison.OrdinalIgnoreCase)
            );

            if (alreadyExists)
                continue;

            extractedCourses.Add(new ExtractedCourse
            {
                Codigo = code,
                Nombre = name,
                Creditos = credits,
                IsSelected = true
            });
        }

        return extractedCourses;
    }

    private string CleanExtractedText(string text)
    {
        text = text.Replace("\r", "\n");

        text = Regex.Replace(text, @"[ \t]+", " ");

        text = Regex.Replace(text, @"\n+", "\n");

        return text;
    }

    private string CleanCourseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        name = name.Replace("\n", " ");

        name = Regex.Replace(name, @"\s+", " ").Trim();

        string[] stopWords =
        {
            "Firma del estudiante",
            "Firma del profesor",
            "TOTAL",
            "PROMEDIO",
            "FECHA",
            "NOTAS",
            "Cursos en Progreso",
            "Cursos a Matricular",
            "Comentarios",
            "(R)Requisito",
            "(C) Concurrente"
        };

        foreach (string stopWord in stopWords)
        {
            int index = name.IndexOf(
                stopWord,
                StringComparison.OrdinalIgnoreCase
            );

            if (index >= 0)
            {
                name = name.Substring(0, index).Trim();
            }
        }

        return name;
    }
}