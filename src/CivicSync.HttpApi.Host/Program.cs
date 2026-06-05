using Autofac.Extensions.DependencyInjection;
using CivicSync.Node.Api;
using CivicSync.Node.Api.Infrastructure.Errors;
using CivicSync.Node.Api.Infrastructure.Persistence.Seed;
using CivicSync.Node.Api.Infrastructure.Security;
using Volo.Abp;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseAutofac();
builder.Services.AddApplication<CivicSyncNodeApiModule>();

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

app.UseApiKeyAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
