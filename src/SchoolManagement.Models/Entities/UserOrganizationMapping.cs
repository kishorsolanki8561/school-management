namespace SchoolManagement.Models.Entities;

public sealed class UserOrganizationMapping : BaseEntity
{
    public int           UserId       { get; init; }
    public int           OrgId        { get; init; }
    public User?         User         { get; init; }
    public Organization? Organization { get; init; }
}
