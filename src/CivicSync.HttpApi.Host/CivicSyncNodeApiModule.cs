using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Application.Services.Audit;
using CivicSync.Node.Api.Application.Services.ChangeRequests;
using CivicSync.Node.Api.Application.Services.Citizens;
using CivicSync.Node.Api.Application.Services.Ledger;
using CivicSync.Node.Api.Application.Services.Nodes;
using CivicSync.Node.Api.Application.Services.Sync;
using CivicSync.Node.Api.Infrastructure.Persistence;
using CivicSync.Node.Api.Infrastructure.Persistence.Seed;
using CivicSync.Node.Api.Infrastructure.Security;
using CivicSync.Node.Api.Infrastructure.Swagger;
using Microsoft.OpenApi;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;

namespace CivicSync.Node.Api;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule),
    typeof(CivicSyncEntityFrameworkCoreModule),
    typeof(AbpSwashbuckleModule))]
public sealed class CivicSyncNodeApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var services = context.Services;

        services.AddControllers();
        services.Configure<NodeOptions>(configuration.GetSection(NodeOptions.SectionName));
        services.Configure<ApiKeyOptions>(configuration.GetSection(ApiKeyOptions.SectionName));

        services.AddScoped<NodeDataSeeder>();
        services.AddScoped<ICitizenService, CitizenService>();
        services.AddScoped<IChangeRequestService, ChangeRequestService>();
        services.AddScoped<IDepartmentNodeService, DepartmentNodeService>();
        services.AddScoped<ILedgerService, LedgerService>();
        services.AddScoped<IDepartmentUserService, DepartmentUserService>();
        services.AddSingleton<INodeSyncSignatureService, NodeSyncSignatureService>();
        services.AddHttpClient<IAuditService, AuditService>();
        services.AddHttpClient<ISyncService, SyncService>();

        services.AddOpenApi();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(ApiKeyAuthenticationMiddleware.HeaderName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = ApiKeyAuthenticationMiddleware.HeaderName,
                In = ParameterLocation.Header,
                Description = "Enter the CivicSync API key used by local node endpoints."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(ApiKeyAuthenticationMiddleware.HeaderName, document, null)] = []
            });
            options.OperationFilter<CivicSyncSwaggerOperationFilter>();
        });
    }
}
