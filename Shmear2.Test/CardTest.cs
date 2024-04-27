using Shmear2.Business.Database;
using Shmear2.Business.Services;
using Shmear2.Business.Database.Models;

namespace Shmear2.Test
{
    public class CardTest : BaseShmearTest
    {    
	    [Fact]   
        public void SeedTest()
        {      
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            shmearService.SeedValues();
            shmearService.SeedSuits();
            shmearService.SeedCards();
            
            cardDbContext.Game.Add(new Game()
            {
               StartedDate = DateTime.Now,
            });

        }
    }
}
