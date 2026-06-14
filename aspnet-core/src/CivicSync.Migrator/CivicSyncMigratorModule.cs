using CivicSync.Core.Configuration;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace CivicSync.Migrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(CivicSyncEntityFrameworkCoreModule))]
public sealed class CivicSyncMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        context.Services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        context.Services.Configure<NodeOptions>(configuration.GetSection(NodeOptions.SectionName));
        context.Services.AddScoped<NodeDataSeeder>();
    }
}
