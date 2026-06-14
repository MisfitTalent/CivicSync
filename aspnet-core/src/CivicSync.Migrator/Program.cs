using Autofac.Extensions.DependencyInjection;
using CivicSync.Core.Configuration;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence.Seed;
using CivicSync.Migrator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
    var databaseOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

    if (databaseOptions.IsPostgreSql())
    {
        Console.WriteLine("Creating CivicSync PostgreSQL schema from the EF model...");
        var postgresSchema = CivicSyncDbContext.TryGetPostgreSqlSchema(
            dbContext.Database.ProviderName,
            dbContext.Database.GetDbConnection().ConnectionString);

        if (!string.IsNullOrWhiteSpace(postgresSchema))
        {
            var createSchemaSql = $@"CREATE SCHEMA IF NOT EXISTS ""{postgresSchema}"";";
            await dbContext.Database.ExecuteSqlRawAsync(createSchemaSql);

            if (!await SchemaHasCoreTablesAsync(dbContext, postgresSchema))
            {
                await dbContext.Database.ExecuteSqlRawAsync(dbContext.Database.GenerateCreateScript());
            }

            await EnsurePostgreSqlSchemaCompatibilityAsync(dbContext, postgresSchema);
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
    }
    else
    {
        Console.WriteLine("Applying CivicSync SQL Server database migrations...");
        await dbContext.Database.MigrateAsync();
    }

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

static async Task<bool> SchemaHasCoreTablesAsync(CivicSyncDbContext dbContext, string schema)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldCloseConnection = connection.State == System.Data.ConnectionState.Closed;

    if (shouldCloseConnection)
    {
        await connection.OpenAsync();
    }

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = @schema
                  AND table_name = 'DepartmentNodes'
            );
            """;

        var schemaParameter = command.CreateParameter();
        schemaParameter.ParameterName = "schema";
        schemaParameter.Value = schema;
        command.Parameters.Add(schemaParameter);

        var result = await command.ExecuteScalarAsync();
        return result is true;
    }
    finally
    {
        if (shouldCloseConnection)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task EnsurePostgreSqlSchemaCompatibilityAsync(CivicSyncDbContext dbContext, string schema)
{
    var alterBiometricReferenceSql = $@"
        ALTER TABLE ""{schema}"".""Citizens""
        ALTER COLUMN ""BiometricReference"" TYPE character varying(1500);
        ";

    await dbContext.Database.ExecuteSqlRawAsync(alterBiometricReferenceSql);
}
