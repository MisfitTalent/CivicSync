using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CivicSync.EntityFrameworkCore.Infrastructure.Persistence;

public sealed class CivicSyncDbContextFactory : IDesignTimeDbContextFactory<CivicSyncDbContext>
{
    public CivicSyncDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CivicSyncDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CivicSyncNode")
            ?? "Server=localhost;Database=CivicSync_HomeAffairs;Trusted_Connection=True;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString);

        return new CivicSyncDbContext(optionsBuilder.Options);
    }
}
