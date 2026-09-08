namespace SchoolManagement.Domain.Entities;

/// <summary>
/// Base type for all persisted entities.
/// </summary>
/// <remarks>
/// Timestamps are stored in local time: the system is deployed on a single site
/// and every financial report (daily collection, month closing) is expressed in
/// the school's own calendar day.
/// </remarks>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
