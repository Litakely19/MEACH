using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Infrastructure.Data;

/// <summary>
/// Sample training centre: L1/L2/L3 groups with independent schedules, students,
/// individual StudentFee obligations, partial payments, receipts and attendance.
/// </summary>
internal sealed class DemoDataSeeder
{
    public const string DemoPassword = "Demo123!";

    private readonly SchoolDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger _logger;

    public DemoDataSeeder(
        SchoolDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await _context.Students.AnyAsync(cancellationToken)
            || await _context.StudentGroups.AnyAsync(cancellationToken)
            || await _context.StudentFees.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Skipping demo data because the roster already contains records");
            return;
        }

        var today = DateTime.Today;
        var roles = await _context.Roles.ToListAsync(cancellationToken);
        var paymentTypes = await _context.PaymentTypes.ToListAsync(cancellationToken);
        var levels = await _context.AcademicLevels.ToListAsync(cancellationToken);
        var schoolYear = await _context.SchoolYears.FirstAsync(year => year.IsCurrent, cancellationToken);
        var admin = await _context.Users.FirstAsync(
            user => user.Username == DatabaseInitializer.DefaultAdministratorUsername,
            cancellationToken);

        var droit = RequireType(paymentTypes, WellKnownPaymentTypes.Droit);
        var ecolage = RequireType(paymentTypes, WellKnownPaymentTypes.Ecolage);
        var livre = RequireType(paymentTypes, WellKnownPaymentTypes.Livre);
        var mockExam = RequireType(paymentTypes, WellKnownPaymentTypes.MockExam);
        var officialExam = RequireType(paymentTypes, WellKnownPaymentTypes.OfficialExam);

        var l1 = RequireLevel(levels, WellKnownAcademicLevels.L1);
        var l2 = RequireLevel(levels, WellKnownAcademicLevels.L2);
        var l3 = RequireLevel(levels, WellKnownAcademicLevels.L3);

        await EnrichSchoolSettingsAsync(cancellationToken);
        var users = await EnsureDemoUsersAsync(roles, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var cashier = users.First(user => user.Username == "cashier");
        var groups = SeedGroups(l1, l2, l3);
        await _context.SaveChangesAsync(cancellationToken);

        var students = SeedStudents(groups, schoolYear, today);
        await _context.SaveChangesAsync(cancellationToken);

        SeedFeesAndPayments(students, droit, ecolage, livre, mockExam, officialExam, schoolYear, admin, cashier, today);
        SeedAttendance(groups, students, admin, today);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded demo training-centre data. Additional logins: accountant / cashier / manager (password {Password})",
            DemoPassword);
    }

    private async Task EnrichSchoolSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _context.SchoolSettings.FirstAsync(cancellationToken);
        settings.SchoolName = "MEACH Training Centre";
        settings.Address = "Lot II M 45, Antananarivo";
        settings.PhoneNumber = "+261 34 00 000 00";
        settings.Email = "contact@meach.mg";
        settings.ReceiptFooter = "Thank you for your payment. Keep this receipt.";
        settings.CurrencyCode = "MGA";
        settings.CurrencySymbol = "Ar";
        settings.DefaultDueDay = 10;
        settings.UpdatedAt = DateTime.Now;
    }

    private async Task<List<User>> EnsureDemoUsersAsync(List<Role> roles, CancellationToken cancellationToken)
    {
        User Add(string username, string firstName, string lastName, RoleName roleName)
        {
            var user = new User
            {
                Username = username,
                PasswordHash = _passwordHasher.Hash(DemoPassword),
                FirstName = firstName,
                LastName = lastName,
                Email = $"{username}@meach.mg",
                RoleId = roles.First(role => role.Name == roleName).Id,
                IsActive = true,
                MustChangePassword = false,
                CreatedAt = DateTime.Now
            };
            _context.Users.Add(user);
            return user;
        }

        var existing = await _context.Users.Select(user => user.Username).ToListAsync(cancellationToken);
        var created = new List<User>();

        if (!existing.Contains("accountant"))
        {
            created.Add(Add("accountant", "Aina", "Randria", RoleName.Accountant));
        }

        if (!existing.Contains("cashier"))
        {
            created.Add(Add("cashier", "Hery", "Rakoto", RoleName.Cashier));
        }

        if (!existing.Contains("manager"))
        {
            created.Add(Add("manager", "Mamy", "Andria", RoleName.SchoolManager));
        }

        return created.Count == 0
            ? await _context.Users.ToListAsync(cancellationToken)
            : created;
    }

    private List<StudentGroup> SeedGroups(AcademicLevel l1, AcademicLevel l2, AcademicLevel l3)
    {
        var groups = new List<StudentGroup>
        {
            Group(l1, "L1 Group 09:00 - 10:00", new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0)),
            Group(l1, "L1 Group 10:00 - 11:00", new TimeSpan(10, 0, 0), new TimeSpan(11, 0, 0)),
            Group(l1, "L1 Group 14:00 - 15:00", new TimeSpan(14, 0, 0), new TimeSpan(15, 0, 0)),
            Group(l2, "L2 Group Morning", new TimeSpan(8, 0, 0), new TimeSpan(10, 0, 0)),
            Group(l2, "L2 Group Afternoon", new TimeSpan(14, 0, 0), new TimeSpan(16, 0, 0)),
            Group(l3, "L3 Group Evening", new TimeSpan(17, 0, 0), new TimeSpan(19, 0, 0))
        };

        _context.StudentGroups.AddRange(groups);
        return groups;
    }

    private static StudentGroup Group(AcademicLevel level, string name, TimeSpan start, TimeSpan end)
    {
        var group = new StudentGroup
        {
            AcademicLevel = level,
            AcademicLevelId = level.Id,
            Name = name,
            Description = $"{level.Name} independent cohort",
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            group.Schedules.Add(new ClassSchedule
            {
                DayOfWeek = day,
                StartTime = start,
                EndTime = end,
                CreatedAt = DateTime.Now
            });
        }

        return group;
    }

    private List<Student> SeedStudents(
        List<StudentGroup> groups,
        SchoolYear schoolYear,
        DateTime today)
    {
        var names = new (string First, string Last, Gender Gender)[]
        {
            ("John", "Doe", Gender.Male),
            ("Mary", "Smith", Gender.Female),
            ("Paul", "Martin", Gender.Male),
            ("Anna", "Doe", Gender.Female),
            ("Alice", "Rakoto", Gender.Female),
            ("Robert", "Andria", Gender.Male),
            ("James", "Rasoa", Gender.Male),
            ("Sophie", "Rabe", Gender.Female),
            ("Luc", "Hery", Gender.Male),
            ("Nadia", "Mamy", Gender.Female),
            ("Eric", "Tojo", Gender.Male),
            ("Clara", "Voahirana", Gender.Female)
        };

        var students = new List<Student>();
        var year = schoolYear.StartDate.Year;

        for (var index = 0; index < names.Length; index++)
        {
            var group = groups[index % groups.Count];
            var name = names[index];
            var student = new Student
            {
                StudentNumber = DocumentNumber.Build(DocumentCodes.Student, year, index + 1),
                FirstName = name.First,
                LastName = name.Last,
                Gender = name.Gender,
                DateOfBirth = new DateTime(2004 + (index % 5), 3, 1 + index),
                PhoneNumber = $"034 40 00 {index + 10:00}",
                AcademicLevelId = group.AcademicLevelId,
                StudentGroupId = group.Id,
                StudentGroup = group,
                SchoolYearId = schoolYear.Id,
                EnrollmentDate = schoolYear.StartDate,
                Status = StudentStatus.Active,
                CreatedAt = today.AddDays(-30 + index)
            };
            students.Add(student);
        }

        _context.Students.AddRange(students);
        return students;
    }

    private void SeedFeesAndPayments(
        List<Student> students,
        PaymentType droit,
        PaymentType ecolage,
        PaymentType livre,
        PaymentType mockExam,
        PaymentType officialExam,
        SchoolYear schoolYear,
        User admin,
        User cashier,
        DateTime today)
    {
        var sequence = 1;
        var receiptSequence = 1;
        var year = today.Year;
        var months = new[] { today.Month == 1 ? 12 : today.Month - 1, today.Month };

        foreach (var student in students)
        {
            AddFee(student, droit, 50_000m, today.AddDays(-20), null, null, schoolYear.Id, paid: 50_000m, admin, cashier, today.AddDays(-18), ref sequence, ref receiptSequence);
            AddFee(student, livre, 75_000m, today.AddDays(10), null, null, schoolYear.Id, paid: student.Id % 3 == 0 ? 0m : 75_000m, admin, cashier, today.AddDays(-5), ref sequence, ref receiptSequence);
            AddFee(student, mockExam, 100_000m, today.AddDays(20), null, null, schoolYear.Id, paid: student.Id % 4 == 0 ? 50_000m : 100_000m, admin, cashier, today.AddDays(-2), ref sequence, ref receiptSequence);
            AddFee(student, officialExam, 200_000m, today.AddDays(40), null, null, schoolYear.Id, paid: student.Id % 5 == 0 ? 0m : 100_000m, admin, cashier, today, ref sequence, ref receiptSequence);

            foreach (var month in months)
            {
                var feeYear = month > today.Month ? year - 1 : year;
                var due = new DateTime(feeYear, month, 10);
                var expected = ecolage.DefaultAmount;
                var paid = month < today.Month ? expected : student.Id % 2 == 0 ? expected / 3 : 0m;
                AddFee(student, ecolage, expected, due, month, feeYear, null, paid, admin, cashier, due.AddDays(2), ref sequence, ref receiptSequence);
            }
        }
    }

    private void AddFee(
        Student student,
        PaymentType type,
        decimal expected,
        DateTime dueDate,
        int? month,
        int? year,
        int? schoolYearId,
        decimal paid,
        User createdBy,
        User cashier,
        DateTime paymentDate,
        ref int paymentSequence,
        ref int receiptSequence)
    {
        var fee = new StudentFee
        {
            Student = student,
            PaymentType = type,
            ExpectedAmount = expected,
            PaidAmount = 0m,
            DueDate = dueDate,
            Month = month,
            Year = year,
            SchoolYearId = schoolYearId,
            IsMandatory = true,
            Notes = schoolYearId is null ? null : type.Name,
            CreatedByUser = createdBy,
            CreatedAt = dueDate.AddDays(-5)
        };

        if (paid > 0)
        {
            var remaining = paid;
            while (remaining > 0)
            {
                var amount = remaining > expected / 2 && remaining != expected
                    ? Math.Round(remaining / 2, 0)
                    : remaining;

                var payment = new Payment
                {
                    PaymentNumber = DocumentNumber.Build(DocumentCodes.Payment, paymentDate.Year, paymentSequence++),
                    Student = student,
                    Amount = amount,
                    PaymentDate = paymentDate,
                    PaymentMethod = PaymentMethod.Cash,
                    ReceivedByUser = cashier,
                    Status = PaymentStatus.Active,
                    CreatedAt = paymentDate,
                    Receipt = new Receipt
                    {
                        ReceiptNumber = DocumentNumber.Build(DocumentCodes.Receipt, paymentDate.Year, receiptSequence++),
                        IssueDate = paymentDate,
                        IssuedByUser = cashier,
                        PrintCount = 0,
                        CreatedAt = paymentDate
                    }
                };

                fee.Payments.Add(payment);
                student.Payments.Add(payment);
                fee.PaidAmount += amount;
                remaining -= amount;
            }
        }

        fee.Status = ResolveStatus(fee, DateTime.Today);
        _context.StudentFees.Add(fee);
    }

    private static FeeStatus ResolveStatus(StudentFee fee, DateTime today)
    {
        if (fee.PaidAmount >= fee.ExpectedAmount)
        {
            return FeeStatus.Paid;
        }

        if (fee.DueDate.Date < today.Date)
        {
            return FeeStatus.Overdue;
        }

        return fee.PaidAmount > 0 ? FeeStatus.PartiallyPaid : FeeStatus.Unpaid;
    }

    private void SeedAttendance(
        List<StudentGroup> groups,
        List<Student> students,
        User recorder,
        DateTime today)
    {
        foreach (var group in groups)
        {
            var schedule = group.Schedules.FirstOrDefault(item => item.DayOfWeek == today.DayOfWeek)
                ?? group.Schedules.First();
            var members = students.Where(student => student.StudentGroupId == group.Id).ToList();
            var index = 0;

            foreach (var student in members)
            {
                var status = (index++ % 4) switch
                {
                    1 => AttendanceStatus.Late,
                    2 => AttendanceStatus.Absent,
                    3 => AttendanceStatus.Excused,
                    _ => AttendanceStatus.Present
                };

                _context.Attendances.Add(new Attendance
                {
                    Student = student,
                    ClassSchedule = schedule,
                    AttendanceDate = today,
                    Status = status,
                    RecordedByUser = recorder,
                    CreatedAt = today
                });
            }
        }
    }

    private static PaymentType RequireType(IEnumerable<PaymentType> types, string name) =>
        types.FirstOrDefault(type => string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Payment type '{name}' was not seeded.");

    private static AcademicLevel RequireLevel(IEnumerable<AcademicLevel> levels, string name) =>
        levels.FirstOrDefault(level => string.Equals(level.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Academic level '{name}' was not seeded.");
}
