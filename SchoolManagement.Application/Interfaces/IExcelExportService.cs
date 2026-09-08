using SchoolManagement.Application.DTOs.Reports;

namespace SchoolManagement.Application.Interfaces;

public interface IExcelExportService
{
    byte[] RenderReport(TabularReport report);
}
