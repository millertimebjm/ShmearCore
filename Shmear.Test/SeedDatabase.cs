using Microsoft.EntityFrameworkCore;
using Shmear.Business.Services;
using Shmear.EntityFramework.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using Xunit;

namespace Shmear.Test
{

    public class SeedDatabase
    {
        private DbContextOptions<CardContext> _contextOptions;

        public SeedDatabase()
        {
            var contextOptions = new DbContextOptionsBuilder<CardContext>();
            contextOptions.UseSqlServer(@"Server=localhost;Database=Card.Dev;Trusted_Connection=True;");
            _contextOptions = contextOptions.Options;
        }

        //public SeedDatabase(DbContextOptions<CardContext> contextOptions)
        //{
        //    _contextOptions = contextOptions;
        //}

        
        public void RunWithOptions(DbContextOptions<CardContext> options)
        {
            _contextOptions = options;
            Run();
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
            Assert.True(CardService.SeedSuits(_contextOptions));
        }

        private void SeedValues()
        {
            Assert.True(CardService.SeedValues(_contextOptions));
        }

        private void SeedCards()
        {
            Assert.True(CardService.SeedCards(_contextOptions));
        }
    }
}
