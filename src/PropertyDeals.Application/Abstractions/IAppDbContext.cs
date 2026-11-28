using Microsoft.EntityFrameworkCore;
using PropertyDeals.Domain.Entities;

namespace PropertyDeals.Application.Abstractions;

public interface IAppDbContext
{

    DbSet<User> Users { get; }
    DbSet<Deal> Deals { get; }
    DbSet<Document> Documents { get; }
    DbSet<Property> Properties { get; }
    DbSet<ActivityLog> ActivityLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
