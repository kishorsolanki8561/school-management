namespace SchoolManagement.Models.Entities;

public sealed class UserRoleMapping : BaseEntity
{
    public int   UserId { get; init; }
    public int   RoleId { get; init; }
    public User? User   { get; init; }
    public Role? Role   { get; init; }
}
