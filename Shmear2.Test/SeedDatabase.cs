using Shmear2.Business.Configuration;
using Shmear2.Business.Database;
using Shmear2.Business.Services;

namespace Shmear2.Test;

public class SeedDatabase : BaseShmearTest
{  
    private const string _connectionString = "SeedDatabase";
    public SeedDatabase(string connectionString = _connectionString)
    {
        var cardDbContext = GenerateCardDbContext(_connectionString);
        _shmearService = new ShmearService(cardDbContext);
    }

    [Fact]
    public void Run()
    {
        SeedValues();
        SeedSuits();
        SeedCards();
    }

    private void SeedSuits()
    {
        Assert.True(_shmearService.SeedSuits());
    }

    private void SeedValues()
    {
        Assert.True(_shmearService.SeedValues());
    }

    private void SeedCards()
    {
        Assert.True(_shmearService.SeedCards());
    }
}