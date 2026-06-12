using Volo.Abp.AspNetCore;
using Volo.Abp.Modularity;

namespace CivicSync.Web.Core;

[DependsOn(typeof(AbpAspNetCoreModule))]
public sealed class CivicSyncWebCoreModule : AbpModule
{
}
