namespace SchoolManagement.Domain.Enums;

/// <summary>
/// Fine grained capabilities granted to roles. Authorization checks in the
/// application and presentation layers are expressed in terms of these values
/// rather than role names, so the role matrix can evolve in one place.
/// </summary>
public enum Permission
{
    ViewDashboard = 1,

    ViewStudents = 10,
    ManageStudents = 11,

    ViewAcademicLevels = 20,
    ManageAcademicLevels = 21,

    ViewStudentGroups = 22,
    ManageStudentGroups = 23,

    ViewSchedules = 24,
    ManageSchedules = 25,

    ViewAttendance = 26,
    ManageAttendance = 27,

    ViewSchoolYears = 40,
    ManageSchoolYears = 41,

    ViewPayments = 50,
    RegisterPayments = 51,
    CancelPayments = 52,

    ViewFees = 60,
    ManageFees = 61,

    ViewReceipts = 70,
    IssueReceipts = 71,

    ViewReports = 80,

    ViewUsers = 90,
    ManageUsers = 91,

    ManageSettings = 100,
    ManagePaymentTypes = 101
}
