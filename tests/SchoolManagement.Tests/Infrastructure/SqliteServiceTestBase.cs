using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.DbInfrastructure.Context;

namespace SchoolManagement.Tests.Infrastructure;

/// <summary>
/// Base class for service tests that require real transaction support.
/// Uses SQLite in-memory (via a persistent connection) instead of the EF InMemory
/// provider, which does not support <see cref="Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction"/>.
/// </summary>
public abstract class SqliteServiceTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly SchoolManagementDbContext _context;

    protected SqliteServiceTestBase()
    {
        // Keep connection open for the lifetime of the test so the in-memory DB survives
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new SchoolManagementDbContext(options);
        _context.Database.EnsureCreated();   // applies EF Core schema (entities + configs)
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}
