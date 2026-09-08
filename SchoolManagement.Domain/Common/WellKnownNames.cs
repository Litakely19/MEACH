namespace SchoolManagement.Domain.Common;

public static class WellKnownAcademicLevels
{
    public const string L1 = "L1";
    public const string L2 = "L2";
    public const string L3 = "L3";

    public static readonly string[] All = [L1, L2, L3];
}

public static class WellKnownPaymentTypes
{
    public const string Droit = "Droit";
    public const string Ecolage = "Écolage";
    public const string Livre = "Livre";
    public const string MockExam = "Mock Exam";
    public const string OfficialExam = "Official Exam";

    public static readonly string[] All = [Droit, Ecolage, Livre, MockExam, OfficialExam];

    /// <summary>Exactly one active line per student per school year (e.g. Droit).</summary>
    public static bool IsOnePerSchoolYear(string paymentTypeName) =>
        string.Equals(paymentTypeName, Droit, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Many lines per student per school year, distinguished by description.
    /// Applies to Livre, exams, and any custom one-time type that is not Droit.
    /// </summary>
    public static bool RequiresDistinctDescription(string paymentTypeName) =>
        !string.IsNullOrWhiteSpace(paymentTypeName) && !IsOnePerSchoolYear(paymentTypeName);

    public static bool IsWellKnown(string paymentTypeName) =>
        All.Any(name => string.Equals(name, paymentTypeName, StringComparison.OrdinalIgnoreCase));

    public static bool IsLivre(string paymentTypeName) =>
        string.Equals(paymentTypeName, Livre, StringComparison.OrdinalIgnoreCase);
}
