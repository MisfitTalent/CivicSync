using CivicSync.Node.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CivicSync.Node.Api.Tests.TestSupport;

internal static class TestDbContextFactory
{
    public static CivicSyncDbContext Create()
    {
        var options = new DbContextOptionsBuilder<CivicSyncDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CivicSyncDbContext(options);
    }
}

