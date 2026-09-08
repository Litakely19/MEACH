namespace SchoolManagement.Domain.Entities;

/// <summary>
/// Marks entities that are hidden instead of removed, so that financial history
/// referencing them stays readable.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTime? DeletedAt { get; set; }
}
