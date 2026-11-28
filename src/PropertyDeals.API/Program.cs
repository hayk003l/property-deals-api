using PropertyDeals.Infrastructure;
using PropertyDeals.Infrastructure.Persistence;
using PropertyDeals.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DevDataSeeder.SeedAsync(dbContext, DateTime.UtcNow);
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
