using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Infrastructure.Data;

public class DatabaseInitializer : IDatabaseInitializer
{
    public const string DefaultAdministratorUsername = "admin";
    public const string DefaultAdministratorPassword = "ChangeMe123!";

    private readonly SchoolDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        SchoolDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);

        await EnsureRolesAsync(cancellationToken);
        await EnsureAdministratorAsync(cancellationToken);
        await EnsureAcademicLevelsAsync(cancellationToken);
        await EnsurePaymentTypesAsync(cancellationToken);
        await EnsureSchoolSettingsAsync(cancellationToken);
        await EnsureCurrentSchoolYearAsync(cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        if (ShouldSeedDemoData())
        {
            await new DemoDataSeeder(_context, _passwordHasher, _logger)
                .SeedAsync(cancellationToken);
        }
    }

    private bool ShouldSeedDemoData()
    {
        var configured = _configuration["Application:SeedDemoData"];
        return bool.TryParse(configured, out var enabled) && enabled;
    }

    private async Task EnsureRolesAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.Roles
            .Select(role => role.Name)
            .ToListAsync(cancellationToken);

        var definitions = new (RoleName Name, string DisplayName, string Description)[]
        {
            (RoleName.Administrator, "Administrator", "Full access to the system and its configuration."),
            (RoleName.Accountant, "Accountant", "Payments, financial information and reports."),
            (RoleName.Cashier, "Cashier", "Registers payments and issues receipts."),
            (RoleName.SchoolManager, "School manager", "Students, academic levels, groups, attendance and reports.")
        };

        foreach (var definition in definitions.Where(definition => !existing.Contains(definition.Name)))
        {
            _context.Roles.Add(new Role
            {
                Name = definition.Name,
                DisplayName = definition.DisplayName,
                Description = definition.Description,
                CreatedAt = DateTime.Now
            });

            _logger.LogInformation("Seeded role {Role}", definition.Name);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAdministratorAsync(CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var administratorRole = await _context.Roles
            .FirstAsync(role => role.Name == RoleName.Administrator, cancellationToken);

        _context.Users.Add(new User
        {
            Username = DefaultAdministratorUsername,
            PasswordHash = _passwordHasher.Hash(DefaultAdministratorPassword),
            FirstName = "System",
            LastName = "Administrator",
            RoleId = administratorRole.Id,
            IsActive = true,
            // The shell blocks navigation until this default password is replaced.
            MustChangePassword = true,
            CreatedAt = DateTime.Now
        });

        _logger.LogWarning(
            "Seeded the default administrator account '{Username}'; its password must be changed at first login",
            DefaultAdministratorUsername);
    }

    private async Task EnsureAcademicLevelsAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.AcademicLevels
            .Select(level => level.Name)
            .ToListAsync(cancellationToken);

        foreach (var name in WellKnownAcademicLevels.All)
        {
            if (existing.Contains(name))
            {
                continue;
            }

            _context.AcademicLevels.Add(new AcademicLevel
            {
                Name = name,
                Description = $"Academic level {name}",
                IsActive = true,
                CreatedAt = DateTime.Now
            });
        }

        if (_context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded academic levels L1, L2 and L3");
        }
    }

    private async Task EnsurePaymentTypesAsync(CancellationToken cancellationToken)
    {
        if (await _context.PaymentTypes.AnyAsync(cancellationToken))
        {
            return;
        }

        _context.PaymentTypes.AddRange(
            new PaymentType
            {
                Name = WellKnownPaymentTypes.Droit,
                Description = "One-time enrolment / droit fee.",
                DefaultAmount = 50_000m,
                Frequency = PaymentFrequency.OneTime,
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            new PaymentType
            {
                Name = WellKnownPaymentTypes.Ecolage,
                Description = "Monthly school fee (écolage).",
                DefaultAmount = 150_000m,
                Frequency = PaymentFrequency.Monthly,
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            new PaymentType
            {
                Name = WellKnownPaymentTypes.Livre,
                Description = "Book fee.",
                DefaultAmount = 75_000m,
                Frequency = PaymentFrequency.OneTime,
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            new PaymentType
            {
                Name = WellKnownPaymentTypes.MockExam,
                Description = "Mock examination fee.",
                DefaultAmount = 100_000m,
                Frequency = PaymentFrequency.OneTime,
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            new PaymentType
            {
                Name = WellKnownPaymentTypes.OfficialExam,
                Description = "Official examination fee.",
                DefaultAmount = 200_000m,
                Frequency = PaymentFrequency.OneTime,
                IsActive = true,
                CreatedAt = DateTime.Now
            });

        _logger.LogInformation("Seeded the default payment types");
    }

    private async Task EnsureSchoolSettingsAsync(CancellationToken cancellationToken)
    {
        if (await _context.SchoolSettings.AnyAsync(cancellationToken))
        {
            return;
        }

        _context.SchoolSettings.Add(new SchoolSetting
        {
            SchoolName = "My School",
            CurrencyCode = "MGA",
            CurrencySymbol = "Ar",
            ReceiptFooter = "Thank you for your payment.",
            DefaultDueDay = 10,
            CreatedAt = DateTime.Now
        });
    }

    /// <summary>
    /// A current school year is required before a student can be enrolled, so one is
    /// derived from today's date: the academic year is assumed to start in September.
    /// </summary>
    private async Task EnsureCurrentSchoolYearAsync(CancellationToken cancellationToken)
    {
        if (await _context.SchoolYears.AnyAsync(cancellationToken))
        {
            return;
        }

        var today = DateTime.Today;
        var startYear = today.Month >= 9 ? today.Year : today.Year - 1;

        _context.SchoolYears.Add(new SchoolYear
        {
            Name = $"{startYear}-{startYear + 1}",
            StartDate = new DateTime(startYear, 9, 1),
            EndDate = new DateTime(startYear + 1, 6, 30),
            IsCurrent = true,
            IsClosed = false,
            CreatedAt = DateTime.Now
        });

        _logger.LogInformation("Seeded the school year {Year}", $"{startYear}-{startYear + 1}");
    }
}
