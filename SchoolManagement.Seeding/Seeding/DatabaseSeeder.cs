namespace SchoolManagement.Seeding.Seeding;

public sealed class DatabaseSeeder
{
    private readonly IEnumerable<ISeeder> _seeders;

    public DatabaseSeeder(IEnumerable<ISeeder> seeders)
        => _seeders = seeders;

    public async Task SeedAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var seeder in _seeders)
        {
            if (await seeder.IsSeededAsync(cancellationToken))
                continue; // Data already exists — skip

            await seeder.SeedAsync(cancellationToken);
        }
    }
}
