using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs.Receipts;
using SchoolManagement.Application.DTOs.Reports;
using SchoolManagement.Application.Interfaces;

namespace SchoolManagement.Infrastructure.Services;

/// <summary>
/// PDF rendering with QuestPDF, used for receipts, invoices and every report.
/// </summary>
public class PdfExportService : IPdfExportService
{
    private const string AccentColor = "#1B5E9C";

    static PdfExportService()
    {
        // Required by QuestPDF before any document is produced.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] RenderReceipt(ReceiptDocument receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(12, Unit.Millimetre);
                page.DefaultTextStyle(style => style.FontSize(9));

                page.Header().Element(header => ComposeSchoolHeader(
                    header,
                    receipt.SchoolName,
                    receipt.SchoolAddress,
                    BuildContactLine(receipt.SchoolPhone, receipt.SchoolEmail)));

                page.Content().PaddingVertical(8).Column(column =>
                {
                    column.Spacing(6);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("PAYMENT RECEIPT").FontSize(13).Bold().FontColor(AccentColor);
                            left.Item().Text($"No. {receipt.ReceiptNumber}").FontSize(11).SemiBold();
                        });

                        row.ConstantItem(170).Column(right =>
                        {
                            right.Item().AlignRight().Text($"Issued on {receipt.IssueDate:dd/MM/yyyy HH:mm}");
                            right.Item().AlignRight().Text($"Payment {receipt.PaymentNumber}");

                            if (receipt.IsReprint)
                            {
                                right.Item().AlignRight().Text($"DUPLICATE (copy #{receipt.PrintCount})")
                                    .Bold()
                                    .FontColor(Colors.Red.Darken1);
                            }

                            if (receipt.PaymentStatus != Domain.Enums.PaymentStatus.Active)
                            {
                                right.Item().AlignRight().Text(receipt.PaymentStatus.ToString().ToUpperInvariant())
                                    .Bold()
                                    .FontColor(Colors.Red.Darken2);
                            }
                        });
                    });

                    column.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Element(box => ComposeFieldBox(box, "Student", new[]
                        {
                            ("Name", receipt.StudentName),
                            ("Student number", receipt.StudentNumber),
                            ("Academic level", receipt.AcademicLevelName),
                            ("Student group", receipt.StudentGroupName)
                        }));

                        row.ConstantItem(10);

                        row.RelativeItem().Element(box => ComposeFieldBox(box, "Payment", new[]
                        {
                            ("Date", receipt.PaymentDate.ToString("dd/MM/yyyy")),
                            ("Method", receipt.PaymentMethod.ToString()),
                            ("Reference", string.IsNullOrWhiteSpace(receipt.Reference) ? "-" : receipt.Reference)
                        }));
                    });

                    column.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            HeaderCell(header.Cell(), "Payment type");
                            HeaderCell(header.Cell(), "Description");
                            HeaderCell(header.Cell(), "Period");
                            HeaderCell(header.Cell(), "Amount", alignRight: true);
                        });

                        foreach (var line in receipt.Lines)
                        {
                            BodyCell(table.Cell(), line.PaymentTypeName);
                            BodyCell(table.Cell(), line.Description);
                            BodyCell(table.Cell(), line.PeriodLabel);
                            BodyCell(table.Cell(), Money.Format(line.Amount, receipt.CurrencySymbol), alignRight: true);
                        }

                        if (receipt.Lines.Count == 0)
                        {
                            BodyCell(table.Cell(), "-");
                            BodyCell(table.Cell(), "-");
                            BodyCell(table.Cell(), "-");
                            BodyCell(
                                table.Cell(),
                                Money.Format(receipt.AmountPaid, receipt.CurrencySymbol),
                                alignRight: true);
                        }
                    });

                    column.Item().PaddingTop(6).AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Amount paid: {Money.Format(receipt.AmountPaid, receipt.CurrencySymbol)}")
                            .FontSize(12)
                            .Bold()
                            .FontColor(AccentColor);

                        totals.Item().Text(
                            $"Obligation {Money.Format(receipt.ObligationTotal, receipt.CurrencySymbol)} - "
                                + $"paid {Money.Format(receipt.ObligationPaid, receipt.CurrencySymbol)} - "
                                + $"remaining {Money.Format(receipt.ObligationRemaining, receipt.CurrencySymbol)}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    column.Item().PaddingTop(14).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text($"Received by: {receipt.ReceivedBy}").SemiBold();
                        });

                        row.ConstantItem(160).Column(right =>
                        {
                            right.Item().AlignCenter().Text("Signature and stamp").FontSize(8);
                            right.Item().PaddingTop(18).LineHorizontal(0.7f).LineColor(Colors.Grey.Medium);
                        });
                    });
                });

                page.Footer().Column(footer =>
                {
                    if (!string.IsNullOrWhiteSpace(receipt.Footer))
                    {
                        footer.Item().AlignCenter().Text(receipt.Footer).FontSize(8).Italic();
                    }

                    footer.Item().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(7).FontColor(Colors.Grey.Medium));
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    public byte[] RenderReport(TabularReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(report.Landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.Margin(14, Unit.Millimetre);
                page.DefaultTextStyle(style => style.FontSize(8));

                page.Header().Column(header =>
                {
                    header.Item().Text(report.SchoolName).FontSize(12).Bold().FontColor(AccentColor);

                    if (!string.IsNullOrWhiteSpace(report.SchoolAddress))
                    {
                        header.Item().Text(report.SchoolAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }

                    header.Item().PaddingTop(6).Text(report.Title).FontSize(13).Bold();

                    if (!string.IsNullOrWhiteSpace(report.Subtitle))
                    {
                        header.Item().Text(report.Subtitle).FontSize(9).FontColor(Colors.Grey.Darken2);
                    }

                    header.Item().PaddingTop(4).LineHorizontal(0.8f).LineColor(AccentColor);
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var column in report.Columns)
                        {
                            columns.RelativeColumn(column.RelativeWidth);
                        }
                    });

                    table.Header(header =>
                    {
                        foreach (var column in report.Columns)
                        {
                            HeaderCell(
                                header.Cell(),
                                column.Header,
                                column.Alignment == ReportColumnAlignment.Right,
                                column.Alignment == ReportColumnAlignment.Center);
                        }
                    });

                    foreach (var row in report.Rows)
                    {
                        for (var index = 0; index < report.Columns.Count; index++)
                        {
                            var alignment = report.Columns[index].Alignment;
                            BodyCell(
                                table.Cell(),
                                index < row.Count ? row[index] : string.Empty,
                                alignment == ReportColumnAlignment.Right,
                                alignment == ReportColumnAlignment.Center);
                        }
                    }

                    if (report.TotalsRow is { Count: > 0 })
                    {
                        for (var index = 0; index < report.Columns.Count; index++)
                        {
                            var alignment = report.Columns[index].Alignment;
                            var value = index < report.TotalsRow.Count ? report.TotalsRow[index] : string.Empty;

                            var cell = table.Cell()
                                .Background(Colors.Grey.Lighten3)
                                .BorderTop(1)
                                .BorderColor(Colors.Grey.Medium)
                                .PaddingVertical(4)
                                .PaddingHorizontal(3);

                            var text = alignment switch
                            {
                                ReportColumnAlignment.Right => cell.AlignRight().Text(value),
                                ReportColumnAlignment.Center => cell.AlignCenter().Text(value),
                                _ => cell.Text(value)
                            };

                            text.Bold();
                        }
                    }
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text(
                            $"Generated on {report.GeneratedAt:dd/MM/yyyy HH:mm} by {report.GeneratedBy}")
                        .FontSize(7)
                        .FontColor(Colors.Grey.Medium);

                    row.ConstantItem(80).AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(7).FontColor(Colors.Grey.Medium));
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void ComposeSchoolHeader(
        IContainer container,
        string schoolName,
        string? address,
        string? contact)
    {
        container.Column(column =>
        {
            column.Item().Text(schoolName).FontSize(14).Bold().FontColor(AccentColor);

            if (!string.IsNullOrWhiteSpace(address))
            {
                column.Item().Text(address).FontSize(8).FontColor(Colors.Grey.Darken1);
            }

            if (!string.IsNullOrWhiteSpace(contact))
            {
                column.Item().Text(contact).FontSize(8).FontColor(Colors.Grey.Darken1);
            }

            column.Item().PaddingTop(4).LineHorizontal(0.8f).LineColor(AccentColor);
        });
    }

    private static void ComposeFieldBox(IContainer container, string title, (string Label, string Value)[] fields)
    {
        container
            .Border(0.7f)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(6)
            .Column(column =>
            {
                column.Item().Text(title).SemiBold().FontColor(AccentColor);

                foreach (var (label, value) in fields)
                {
                    column.Item().Text(text =>
                    {
                        text.Span($"{label}: ").FontColor(Colors.Grey.Darken1);
                        text.Span(value).SemiBold();
                    });
                }
            });
    }

    private static void HeaderCell(IContainer cell, string value, bool alignRight = false, bool alignCenter = false)
    {
        var container = cell
            .Background(AccentColor)
            .PaddingVertical(4)
            .PaddingHorizontal(3);

        container = alignRight ? container.AlignRight() : alignCenter ? container.AlignCenter() : container;
        container.Text(value).FontColor(Colors.White).SemiBold();
    }

    private static void BodyCell(IContainer cell, string value, bool alignRight = false, bool alignCenter = false)
    {
        var container = cell
            .BorderBottom(0.4f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3)
            .PaddingHorizontal(3);

        container = alignRight ? container.AlignRight() : alignCenter ? container.AlignCenter() : container;
        container.Text(value);
    }

    private static string? BuildContactLine(string? phone, string? email)
    {
        var parts = new[] { phone, email }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return parts.Length == 0 ? null : string.Join("  -  ", parts);
    }
}
