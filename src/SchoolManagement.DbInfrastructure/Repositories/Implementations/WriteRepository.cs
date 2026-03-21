using Microsoft.EntityFrameworkCore;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Repositories.Implementations;

public class WriteRepository<T> : IWriteRepository<T> where T : BaseEntity
{
    protected readonly SchoolManagementDbContext Context;

    public WriteRepository(SchoolManagementDbContext context)
    {
        Context = context;
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await Context.Set<T>().AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        Context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public async Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"{typeof(T).Name} with id {id} was not found.");
        entity.IsDeleted = true;
        Context.Set<T>().Update(entity);
    }

    public async Task<int> SaveAsync(CancellationToken cancellationToken = default) =>
        await Context.SaveChangesAsync(cancellationToken);

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Context.Set<T>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
}
