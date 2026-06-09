using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CivicSync.Web.Host.Tests.TestSupport;

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

