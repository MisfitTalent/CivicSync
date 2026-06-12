using CivicSync.Core;
using Volo.Abp.Modularity;

namespace CivicSync.Application;

[DependsOn(typeof(CivicSyncCoreModule))]
public sealed class CivicSyncApplicationModule : AbpModule
{
}
