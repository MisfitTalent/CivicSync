using CivicSync.Core;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.DependencyInjection;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.Modularity;

namespace CivicSync.EntityFrameworkCore.Infrastructure.Persistence;

[DependsOn(
    typeof(CivicSyncCoreModule),
    typeof(AbpEntityFrameworkCoreSqlServerModule))]
public sealed class CivicSyncEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<CivicSyncDbContext>(options =>
        {
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        Configure<AbpDbContextOptions>(options =>
        {
            options.UseSqlServer();
        });
    }
}

