using Shmear2.Business.Database.Models;
using Shmear2.Business.Services;

namespace Shmear2.Test
{
    public class PlayerComputerTest : BaseShmearTest
    {
        [Fact]
        public async Task PlayerComputerTestWager()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            shmearService.SeedValues();
            shmearService.SeedSuits();
            shmearService.SeedCards();
            IPlayerService playerService = new PlayerService(cardDbContext);
            IPlayerComputerService playerComputerService = new PlayerComputerService(
                cardDbContext,
                shmearService,
                playerService);
                
            var game = await shmearService.CreateGame();
            var players = new List<Player>();
            for (int i = 0; i < 4; i++)
            {
                var player = GenerateNewPlayer($"PlayerComputerTestWagerBest{i}");
                await playerService.SavePlayer(player);
                await shmearService.AddPlayer(game.Id, player.Id, i);
                var gamePlayer = await shmearService.GetGamePlayer(game.Id, player.Id);
                gamePlayer.Ready = true;
                await shmearService.SaveGamePlayer(gamePlayer);
                players.Add(player);
            }
            await shmearService.StartGame(game.Id);
            await shmearService.StartRound(game.Id);
            await shmearService.DealCards(game.Id);

            var wager = await playerComputerService.SetWager(game.Id, players.First().Id);
            Assert.True(wager >= 0 && wager < 10);
        }

        [Fact]
        public async Task PlayerComputerTestPlayCard()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            shmearService.SeedValues();
            shmearService.SeedSuits();
            shmearService.SeedCards();
            IPlayerService playerService = new PlayerService(cardDbContext);

            var game = await shmearService.CreateGame();
            var players = new List<Player>();
            for (int i = 0; i < 4; i++)
            {
                var player = GenerateNewPlayer($"PlayerComputerTestWagerBest{i}");
                await playerService.SavePlayer(player);
                await shmearService.AddPlayer(game.Id, player.Id, i);
                var gamePlayer = await shmearService.GetGamePlayer(game.Id, player.Id);
                gamePlayer.Ready = true;
                await shmearService.SaveGamePlayer(gamePlayer);
                players.Add(player);
            }
            await shmearService.StartGame(game.Id);
            await shmearService.StartRound(game.Id);
            await shmearService.DealCards(game.Id);

            var trick = await shmearService.CreateTrick(game.Id);
            await shmearService.GetNextCardGamePlayer(game.Id, trick.Id);
        }
    }
}

