using Autofac.Extensions.DependencyInjection;
using CivicSync.Web.Host;
using CivicSync.Web.Core.Infrastructure.Errors;
using CivicSync.Web.Host.Infrastructure.Persistence.Seed;
using CivicSync.Web.Core.Infrastructure.Security;
using Volo.Abp;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseAutofac();
builder.Services.AddApplication<CivicSyncWebHostModule>();

var app = builder.Build();

await app.InitializeApplicationAsync();

app.UseApiExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CivicSync ABP Node API v1");
        options.RoutePrefix = "swagger";
    });

    await app.SeedNodeDataAsync();
}

app.UseHttpsRedirection();

app.UseCors("CivicSyncFrontend");

app.UseApiKeyAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
