using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs.Students;

public record StudentListItem(
    int Id,
    string StudentNumber,
    string FullName,
    Gender Gender,
    DateTime DateOfBirth,
    string AcademicLevelName,
    string StudentGroupName,
    string? PhoneNumber,
    StudentStatus Status,
    DateTime EnrollmentDate,
    decimal OutstandingBalance);

public record StudentFilter(
    string? SearchTerm = null,
    int? AcademicLevelId = null,
    int? StudentGroupId = null,
    StudentStatus? Status = null,
    int Page = 1,
    int PageSize = 25);

public record CreateStudentRequest(
    string FirstName,
    string LastName,
    Gender Gender,
    DateTime DateOfBirth,
    string? Address,
    string? PhoneNumber,
    string? Email,
    int AcademicLevelId,
    int StudentGroupId,
    int? SchoolYearId,
    DateTime EnrollmentDate,
    StudentStatus Status = StudentStatus.Active);

public record UpdateStudentRequest(
    int Id,
    string FirstName,
    string LastName,
    Gender Gender,
    DateTime DateOfBirth,
    string? Address,
    string? PhoneNumber,
    string? Email,
    int AcademicLevelId,
    int StudentGroupId,
    int? SchoolYearId,
    DateTime EnrollmentDate,
    StudentStatus Status);

public record TransferStudentRequest(int StudentId, int TargetStudentGroupId, string? Reason);

public record StudentFeeSummary(
    int Id,
    string PaymentTypeName,
    string PeriodLabel,
    decimal ExpectedAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateTime DueDate,
    FeeStatus Status);

public record StudentPaymentSummary(
    int Id,
    string PaymentNumber,
    DateTime PaymentDate,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string ReceivedBy,
    PaymentStatus Status,
    string? ReceiptNumber);

public record StudentReceiptSummary(
    int Id,
    string ReceiptNumber,
    DateTime IssueDate,
    decimal Amount,
    string PaymentNumber);

public record StudentScheduleItem(
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string SessionLabel);

public record StudentAttendanceSummary(
    int TotalSessions,
    int Present,
    int Absent,
    int Late,
    int Excused,
    decimal AttendancePercentage);

public record StudentDetail(
    int Id,
    string StudentNumber,
    string FirstName,
    string LastName,
    string FullName,
    Gender Gender,
    DateTime DateOfBirth,
    string? Address,
    string? PhoneNumber,
    string? Email,
    int AcademicLevelId,
    string AcademicLevelName,
    int StudentGroupId,
    string StudentGroupName,
    int? SchoolYearId,
    string? SchoolYearName,
    DateTime EnrollmentDate,
    StudentStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<StudentScheduleItem> Schedule,
    StudentAttendanceSummary Attendance,
    IReadOnlyList<StudentFeeSummary> Fees,
    IReadOnlyList<StudentPaymentSummary> Payments,
    IReadOnlyList<StudentReceiptSummary> Receipts,
    decimal TotalExpected,
    decimal TotalPaid,
    decimal OutstandingBalance);
