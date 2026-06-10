using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace CivicSync.Web.Host.Infrastructure.Persistence.Seed;

public static class NodeDataSeederExtensions
{
    public static async Task SeedNodeDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        var seeder = scope.ServiceProvider.GetRequiredService<NodeDataSeeder>();
        var dbContext = scope.ServiceProvider.GetRequiredService<CivicSyncDbContext>();

        await dbContext.Database.MigrateAsync();

        using var unitOfWork = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
        await seeder.SeedAsync();
        await unitOfWork.CompleteAsync();
    }
}
