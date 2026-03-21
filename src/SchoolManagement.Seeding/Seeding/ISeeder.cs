namespace SchoolManagement.Seeding.Seeding;

public interface ISeeder
{
    Task<bool> IsSeededAsync(CancellationToken cancellationToken = default);
    Task SeedAsync(CancellationToken cancellationToken = default);
}
