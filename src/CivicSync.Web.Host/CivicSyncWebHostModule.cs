using CivicSync.Core.Configuration;
using CivicSync.Application;
using CivicSync.Application.Services.Audit;
using CivicSync.Application.Services.ChangeRequests;
using CivicSync.Application.Services.Citizens;
using CivicSync.Application.Services.Ledger;
using CivicSync.Application.Services.Nodes;
using CivicSync.Application.Services.Sync;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence.Seed;
using CivicSync.Web.Core.Infrastructure.Security;
using CivicSync.Web.Core.Infrastructure.Swagger;
using CivicSync.Web.Core;
using Microsoft.OpenApi;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;

namespace CivicSync.Web.Host;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule),
    typeof(CivicSyncApplicationModule),
    typeof(CivicSyncEntityFrameworkCoreModule),
    typeof(CivicSyncWebCoreModule),
    typeof(AbpSwashbuckleModule))]
public sealed class CivicSyncWebHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var services = context.Services;

        services.AddControllers();
        services.AddCors(options =>
        {
            options.AddPolicy("CivicSyncFrontend", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:5173",
                        "https://localhost:5173",
                        "http://127.0.0.1:5173",
                        "https://127.0.0.1:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
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
