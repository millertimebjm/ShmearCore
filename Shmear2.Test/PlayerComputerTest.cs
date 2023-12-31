using Shmear2.Business.Database.Models;
using Shmear2.Business.Services;

namespace Shmear2.Test
{
    public class PlayerComputerTest : BaseShmearTest
    {
        private const string _connectionString = "PlayerComputerTest";

        public PlayerComputerTest() : base()
        {
            var cardDbContext = GenerateCardDbContext(_connectionString);
            _shmearService = new ShmearService(cardDbContext);
            _playerService = new PlayerService(cardDbContext);
            _playerComputerService = new PlayerComputerService(
                cardDbContext,
                _shmearService,
                _playerService);
        }

        [Fact]
        public async Task PlayerComputerTestWager()
        {
            var seedDatabase = new SeedDatabase(_connectionString);
            seedDatabase.Run();

            var game = await _shmearService.CreateGame();
            var players = new List<Player>();
            for (int i = 0; i < 4; i++)
            {
                var player = GenerateNewPlayer($"PlayerComputerTestWagerBest{i}");
                await _playerService.SavePlayer(player);
                await _shmearService.AddPlayer(game.Id, player.Id, i);
                var gamePlayer = await _shmearService.GetGamePlayer(game.Id, player.Id);
                gamePlayer.Ready = true;
                await _shmearService.SaveGamePlayer(gamePlayer);
                players.Add(player);
            }
            await _shmearService.StartGame(game.Id);
            await _shmearService.StartRound(game.Id);
            await _shmearService.DealCards(game.Id);

            var wager = await _playerComputerService.SetWager(game.Id, players.First().Id);
            Assert.True(wager > 0 && wager < 10);
        }

        [Fact]
        public async Task PlayerComputerTestPlayCard()
        {
            var seedDatabase = new SeedDatabase(_connectionString);
            seedDatabase.Run();

            var game = await _shmearService.CreateGame();
            var players = new List<Player>();
            for (int i = 0; i < 4; i++)
            {
                var player = GenerateNewPlayer($"PlayerComputerTestWagerBest{i}");
                await _playerService.SavePlayer(player);
                await _shmearService.AddPlayer(game.Id, player.Id, i);
                var gamePlayer = await _shmearService.GetGamePlayer(game.Id, player.Id);
                gamePlayer.Ready = true;
                await _shmearService.SaveGamePlayer(gamePlayer);
                players.Add(player);
            }
            await _shmearService.StartGame(game.Id);
            await _shmearService.StartRound(game.Id);
            await _shmearService.DealCards(game.Id);

            var trick = await _shmearService.CreateTrick(game.Id);
            await _shmearService.GetNextCardGamePlayer(game.Id, trick.Id);
        }
    }
}

