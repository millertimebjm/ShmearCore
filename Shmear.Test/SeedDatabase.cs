using Microsoft.EntityFrameworkCore;
using Shmear.Business.Services;
using Shmear.EntityFramework.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using Xunit;

namespace Shmear.Test
{

    public class SeedDatabase
    {
        //private DbContextOptions<CardContext> _contextOptions;

        //public SeedDatabase()
        //{
        //    var contextOptions = new DbContextOptionsBuilder<CardContext>();
        //    contextOptions.UseSqlServer(@"Server=localhost;Database=Card.Dev;Trusted_Connection=True;");
        //    _contextOptions = contextOptions.Options;
        //}

        //public SeedDatabase(DbContextOptions<CardContext> contextOptions)
        //{
        //    _contextOptions = contextOptions;
        //}

        ////public void Run()
        ////{
        ////    seedDatabase.SeedValues();
        ////    seedDatabase.SeedSuits();
        ////    seedDatabase.SeedCards();
        ////}

        //[Fact]
        //public void SeedSuits()
        //{
        //    CardOptions optionsBuilder = new DbContextOptionsBuilder<CardContext>();
        //    optionsBuilder.UseSqlServer(@"Server=localhost;Database=Card.Dev;Trusted_Connection=True;");
        //    Assert.True(CardService.SeedSuits(optionsBuilder.Options));
        //}

        //[Fact]
        //public void SeedValues()
        //{
        //    var cardService = new CardService(_contextOptions);
        //    Assert.True(cardService.SeedValues());
        //}

        //[Fact]
        //public void SeedCards()
        //{
        //    var cardService = new CardService(_contextOptions);
        //    Assert.True(cardService.SeedCards());
        //}
    }
}
