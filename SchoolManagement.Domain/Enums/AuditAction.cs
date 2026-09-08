namespace SchoolManagement.Domain.Enums;

public enum AuditAction
{
    Login = 1,
    Logout = 2,
    LoginFailed = 3,
    PasswordChanged = 4,

    Created = 10,
    Updated = 11,
    Deleted = 12,

    PaymentRegistered = 20,
    PaymentCancelled = 21,
    PaymentReversed = 22,

    FeeCreated = 30,
    FeeAdjusted = 31,
    FeeCancelled = 32,

    ReceiptPrinted = 40,

    SchoolYearClosed = 50,
    SchoolYearReopened = 51,

    AttendanceRecorded = 60,
    StudentTransferred = 70
}
