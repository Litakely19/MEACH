using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Infrastructure.Data.Converters;

namespace SchoolManagement.Infrastructure.Data;

public class SchoolDbContext : DbContext
{
    public SchoolDbContext(DbContextOptions<SchoolDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<SchoolYear> SchoolYears => Set<SchoolYear>();

    public DbSet<AcademicLevel> AcademicLevels => Set<AcademicLevel>();

    public DbSet<StudentGroup> StudentGroups => Set<StudentGroup>();

    public DbSet<ClassSchedule> ClassSchedules => Set<ClassSchedule>();

    public DbSet<Attendance> Attendances => Set<Attendance>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<PaymentType> PaymentTypes => Set<PaymentType>();

    public DbSet<StudentFee> StudentFees => Set<StudentFee>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Receipt> Receipts => Set<Receipt>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<SchoolSetting> SchoolSettings => Set<SchoolSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchoolDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HaveConversion<MoneyConverter>();
        configurationBuilder.Properties<string>().HaveMaxLength(256);
        base.ConfigureConventions(configurationBuilder);
    }
}
