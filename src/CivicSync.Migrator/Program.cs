using Autofac.Extensions.DependencyInjection;
using CivicSync.Node.Api.Infrastructure.Persistence;
using CivicSync.Node.Api.Infrastructure.Persistence.Seed;
using CivicSync.Node.Api.Migrator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.Uow;

var builder = Host.CreateDefaultBuilder(args)
    .UseAutofac()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddApplication<CivicSyncMigratorModule>(options =>
        {
            options.Services.ReplaceConfiguration(hostContext.Configuration);
        });
    });

using var host = builder.Build();
await host.InitializeAsync();

try
{
    using var scope = host.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CivicSyncDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<NodeDataSeeder>();
    var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

    Console.WriteLine("Applying CivicSync database migrations...");
    await dbContext.Database.MigrateAsync();

    Console.WriteLine("Seeding CivicSync node data...");
    using var unitOfWork = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
    await seeder.SeedAsync();
    await unitOfWork.CompleteAsync();

    Console.WriteLine("CivicSync database migration completed successfully.");
}
finally
{
    await host.StopAsync();
}
