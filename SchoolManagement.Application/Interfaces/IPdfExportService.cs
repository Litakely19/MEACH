using SchoolManagement.Application.DTOs.Receipts;
using SchoolManagement.Application.DTOs.Reports;

namespace SchoolManagement.Application.Interfaces;

public interface IPdfExportService
{
    byte[] RenderReceipt(ReceiptDocument receipt);

    byte[] RenderReport(TabularReport report);
}
