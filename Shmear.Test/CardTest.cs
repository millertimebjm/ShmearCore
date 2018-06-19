using Microsoft.EntityFrameworkCore;
using Shmear.Business.Services;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Shmear.Test
{
    public class CardTest
    {
        private DbContextOptions<CardContext> _contextOptions;
        public CardTest()
        {
            var optionsBuilder = new DbContextOptionsBuilder<CardContext>();
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
            _contextOptions = optionsBuilder.Options;
            var seedDatabase = new SeedDatabase(_contextOptions);
            //seedDatabase.Run();
        }

        [Fact]
        public void JokerTest()
        {
            //var cardService = new CardService(_contextOptions);
        }
    }
}
