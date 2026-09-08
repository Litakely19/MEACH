namespace SchoolManagement.Domain.Entities;

public class AcademicLevel : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<StudentGroup> StudentGroups { get; set; } = new List<StudentGroup>();

    public ICollection<Student> Students { get; set; } = new List<Student>();
}
