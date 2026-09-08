namespace SchoolManagement.Application.DTOs.Reports;

public enum ReportColumnAlignment
{
    Left = 0,
    Right = 1,
    Center = 2
}

public record ReportColumn(string Header, ReportColumnAlignment Alignment = ReportColumnAlignment.Left, float RelativeWidth = 1f);

/// <summary>
/// Presentation neutral table used for every PDF and Excel export.
/// </summary>
/// <remarks>
/// Each report screen projects its own rows into this shape, which keeps a single
/// PDF renderer and a single Excel writer for the whole reporting module instead
/// of one exporter per report.
/// </remarks>
public record TabularReport(
    string Title,
    string? Subtitle,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<string>? TotalsRow,
    string SchoolName,
    string? SchoolAddress,
    DateTime GeneratedAt,
    string GeneratedBy,
    bool Landscape = false);
