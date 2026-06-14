using Autofac.Extensions.DependencyInjection;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence.Seed;
using CivicSync.Migrator;
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

    Console.WriteLine("Applying CivicSync SQL Server database migrations...");
    await dbContext.Database.MigrateAsync();

    Console.WriteLine("Seeding CivicSync node data...");
    using var unitOfWork = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
    await seeder.SeedAsync();
    await unitOfWork.CompleteAsync();

    Console.WriteLine("CivicSync database setup completed successfully.");
}
finally
{
    await host.StopAsync();
}
