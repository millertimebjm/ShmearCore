using Microsoft.EntityFrameworkCore;
using Shmear2.Business.Database;
using Shmear2.Business.Services;

namespace Shmear2.Test;

public class SeedDatabaseTest : BaseShmearTest
{
    [Fact]
    public void Run()
    {
        var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
        IShmearService shmearService = new ShmearService(cardDbContext);
        SeedValues(shmearService);
        SeedSuits(shmearService);
        SeedCards(shmearService);
    }

    private static void SeedSuits(IShmearService shmearService)
    {
        Assert.True(shmearService.SeedSuits());
    }

    private static void SeedValues(IShmearService shmearService)
    {
        Assert.True(shmearService.SeedValues());
    }

    private static void SeedCards(IShmearService shmearService)
    {
        Assert.True(shmearService.SeedCards());
    }
}