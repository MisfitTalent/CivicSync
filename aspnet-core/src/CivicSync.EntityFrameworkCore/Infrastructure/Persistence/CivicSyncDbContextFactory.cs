using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using CivicSync.Core.Configuration;

namespace CivicSync.EntityFrameworkCore.Infrastructure.Persistence;

public sealed class CivicSyncDbContextFactory : IDesignTimeDbContextFactory<CivicSyncDbContext>
{
    public CivicSyncDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CivicSyncDbContext>();
        var provider = Environment.GetEnvironmentVariable("Database__Provider") ?? DatabaseOptions.SqlServerProvider;
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CivicSyncNode")
            ?? "Server=localhost,1433;Database=CivicSync_HomeAffairs;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True";

        if (string.Equals(provider, DatabaseOptions.PostgreSqlProvider, StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseNpgsql(connectionString);
        }
        else
        {
            optionsBuilder.UseSqlServer(connectionString);
        }

        return new CivicSyncDbContext(optionsBuilder.Options);
    }
}
