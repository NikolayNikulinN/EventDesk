using EventDesk.Application.Abstractions.Persistence;

namespace EventDesk.Infrastructure.Persistence;

public sealed class UnitOfWork(EventDeskDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
