using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SchoolManagement.Domain.Exceptions;

namespace SchoolManagement.WPF.Services;

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;
    }

    public string PdfFilter => "PDF document (*.pdf)|*.pdf";

    public string ExcelFilter => "Excel workbook (*.xlsx)|*.xlsx";

    public string DatabaseFilter => "SQLite database (*.db)|*.db";

    public string ImageFilter => "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";

    public string? AskForSavePath(string suggestedFileName, string filter)
    {
        var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = filter,
            AddExtension = true,
            OverwritePrompt = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? AskForFileToOpen(string filter)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public async Task<string?> SaveAndOpenAsync(byte[] content, string suggestedFileName, string filter)
    {
        var path = AskForSavePath(suggestedFileName, filter);
        if (path is null)
        {
            return null;
        }

        await SaveAsync(path, content);
        OpenWithDefaultApplication(path);
        return path;
    }

    public async Task SaveAsync(string path, byte[] content, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(path, content, cancellationToken);
        _logger.LogInformation("Document written to {Path}", path);
    }

    public void OpenWithDefaultApplication(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not open {Path}", path);
            throw new DomainException(
                "The document was saved but could not be opened. Open it manually from its folder.");
        }
    }

    public void Print(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path)
            {
                Verb = "print",
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Printing {Path} failed; falling back to opening the document", path);

            // Not every PDF reader registers a print verb; opening the document lets
            // the user print from the reader itself.
            OpenWithDefaultApplication(path);
        }
    }
}
