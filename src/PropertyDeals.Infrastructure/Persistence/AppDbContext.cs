using Microsoft.EntityFrameworkCore;
using PropertyDeals.Application.Abstractions;
using PropertyDeals.Domain.Entities;
using PropertyDeals.Infrastructure.Outbox;

namespace PropertyDeals.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Deal> Deals => Set<Deal>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
