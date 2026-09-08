using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Authorization;

/// <summary>
/// Single source of truth for what each role is allowed to do. Keeping the matrix
/// in the domain (instead of in the database) means an upgrade cannot leave a
/// deployment with a silently over privileged role.
/// </summary>
public static class RolePermissionMatrix
{
    private static readonly IReadOnlyDictionary<RoleName, HashSet<Permission>> Matrix =
        new Dictionary<RoleName, HashSet<Permission>>
        {
            [RoleName.Administrator] = new(Enum.GetValues<Permission>()),

            [RoleName.Accountant] = new()
            {
                Permission.ViewDashboard,
                Permission.ViewStudents,
                Permission.ViewAcademicLevels,
                Permission.ViewStudentGroups,
                Permission.ViewSchedules,
                Permission.ViewSchoolYears,
                Permission.ViewPayments,
                Permission.ViewFees,
                Permission.ManageFees,
                Permission.ViewReceipts,
                Permission.ViewReports
            },

            [RoleName.Cashier] = new()
            {
                Permission.ViewDashboard,
                Permission.ViewStudents,
                Permission.ViewAcademicLevels,
                Permission.ViewStudentGroups,
                Permission.ViewPayments,
                Permission.RegisterPayments,
                Permission.ViewFees,
                Permission.ViewReceipts,
                Permission.IssueReceipts
            },

            [RoleName.SchoolManager] = new()
            {
                Permission.ViewDashboard,
                Permission.ViewStudents,
                Permission.ManageStudents,
                Permission.ViewAcademicLevels,
                Permission.ManageAcademicLevels,
                Permission.ViewStudentGroups,
                Permission.ManageStudentGroups,
                Permission.ViewSchedules,
                Permission.ManageSchedules,
                Permission.ViewAttendance,
                Permission.ManageAttendance,
                Permission.ViewSchoolYears,
                Permission.ViewPayments,
                Permission.ViewFees,
                Permission.ViewReceipts,
                Permission.ViewReports
            }
        };

    public static bool HasPermission(RoleName role, Permission permission) =>
        Matrix.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    public static IReadOnlySet<Permission> GetPermissions(RoleName role) =>
        Matrix.TryGetValue(role, out var permissions)
            ? permissions
            : new HashSet<Permission>();
}
