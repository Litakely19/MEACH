using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public class Role : BaseEntity
{
    public RoleName Name { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
