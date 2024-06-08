using Microsoft.EntityFrameworkCore;
using Shmear2.Business.Database;
using Shmear2.Business.Services;

namespace Shmear2.Test;

public class SeedDatabaseTest : BaseShmearTest
{
    [Fact]
    public async Task Run()
    {
        var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
        IShmearService shmearService = new ShmearService(cardDbContext);
        await SeedValues(shmearService);
        await SeedSuits(shmearService);
        await SeedCards(shmearService);
    }

    private static async Task SeedSuits(IShmearService shmearService)
    {
        Assert.True(await shmearService.SeedSuits());
    }

    private static async Task SeedValues(IShmearService shmearService)
    {
        Assert.True(await shmearService.SeedValues());
    }

    private static async Task SeedCards(IShmearService shmearService)
    {
        Assert.True(await shmearService.SeedCards());
    }
}