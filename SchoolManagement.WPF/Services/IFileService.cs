namespace SchoolManagement.WPF.Services;

public interface IFileService
{
    /// <summary>Asks where to save a document and returns the chosen path, or null when cancelled.</summary>
    string? AskForSavePath(string suggestedFileName, string filter);

    string? AskForFileToOpen(string filter);

    /// <summary>Writes the bytes and opens the file with the program registered for its extension.</summary>
    Task<string?> SaveAndOpenAsync(byte[] content, string suggestedFileName, string filter);

    Task SaveAsync(string path, byte[] content, CancellationToken cancellationToken = default);

    void OpenWithDefaultApplication(string path);

    /// <summary>Sends a PDF straight to the printing dialog of the default viewer.</summary>
    void Print(string path);

    string PdfFilter { get; }

    string ExcelFilter { get; }

    string DatabaseFilter { get; }

    string ImageFilter { get; }
}
