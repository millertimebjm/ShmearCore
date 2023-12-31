using Shmear2.Business.Services;
using Shmear2.Business.Database.Models;
using Shmear2.Business.Database;
using System.Linq;

namespace Shmear2.Test
{
    public class GameTest : BaseShmearTest
    {
        private const string _connectionString = "GameTest";
        private readonly CardDbContext _cardDbContext;
        public GameTest() 
        {
            _cardDbContext = GenerateCardDbContext(_connectionString);
            _shmearService = new ShmearService(_cardDbContext);
            _playerService = new PlayerService(_cardDbContext);
            _playerComputerService = new PlayerComputerService(
                _cardDbContext,
                _shmearService,
                _playerService
            );
        }

        // [Fact]
        // public async Task GameTestNew()
        // {
        //     var game = await _shmearService.CreateGame();
        //     Assert.True(game.Id > 0);
        //     Assert.True(game.CreatedDate > DateTime.Now.AddMinutes(-1) && game.CreatedDate < DateTime.Now);
        //     Assert.True(game.StartedDate == null);
        //     Assert.True(game.Team1Points == 0);
        //     Assert.True(game.Team2Points == 0);
        // }

        // [Fact]
        // public async Task GameTestOpen()
        // {
        //     var game = await _shmearService.CreateGame();
        //     var openGame = await _shmearService.GetOpenGame();
        //     Assert.True(openGame.StartedDate == null);
        // }

        // [Fact]
        // public async Task GameTestGet()
        // {
        //     var game = await _shmearService.CreateGame();
        //     var newGame = await _shmearService.GetGame(game.Id);
        //     Assert.True(game.Id == newGame.Id);
        // }

        // [Fact]
        // public async Task GameTestAddPlayer()
        // {
        //     var game = await _shmearService.CreateGame();
        //     var player = GenerateNewPlayer($"GameTestAddPlayer");
        //     player = await _playerService.SavePlayer(player);
        //     var gameAddPlayerChange = await _shmearService.AddPlayer(game.Id, player.Id, 0);

        //     // Player Keepalive and Game Player Add is two different records being changed
        //     Assert.True(gameAddPlayerChange == 2);
        // }

        // [Fact]
        // public async Task GameTestGetGamePlayer()
        // {
        //     var game = await _shmearService.CreateGame();
        //     var player = GenerateNewPlayer($"GameTestGetGamePlayer");
        //     player = await _playerService.SavePlayer(player);
        //     await _shmearService.AddPlayer(game.Id, player.Id, 0);
        //     var newPlayer = await _shmearService.GetGamePlayer(game.Id, player.Id);
        //     Assert.True(newPlayer.PlayerId == player.Id);
        // }

        // [Fact]
        // public async Task GameTestGetGamePlayers()
        // {
        //     var game = await _shmearService.CreateGame();
        //     var players = new List<Player>();
        //     for (int i = 0; i < 4; i++)
        //     {
        //         var player = GenerateNewPlayer($"GameTestGetGamePlayers{i}");
        //         player = await _playerService.SavePlayer(player);
        //         await _shmearService.AddPlayer(game.Id, player.Id, i);
        //         players.Add(player);
        //     }

        //     var gamePlayers = await _shmearService.GetGamePlayers(game.Id);
        //     Assert.True(gamePlayers.Count() == 4);
        //     foreach (var gamePlayer in gamePlayers)
        //     {
        //         Assert.Contains(gamePlayer.PlayerId, players.Select(_ => _.Id));
        //     }
        // }

        // [Fact]
        // public async Task GameTestGetPlayersByGame()
        // {
        //     var game = await _shmearService.CreateGame();
        //     var players = new List<Player>();
        //     for (int i = 0; i < 4; i++)
        //     {
        //         var player = GenerateNewPlayer($"GameTestGetGamePlayers{i}");
        //         player = await _playerService.SavePlayer(player);
        //         await _shmearService.AddPlayer(game.Id, player.Id, i);
        //         players.Add(player);
        //     }

        //     var newPlayers = await _shmearService.GetPlayersByGameAsync(game.Id);
        //     Assert.True(newPlayers.Count() == 4);
        //     foreach (var newPlayer in newPlayers)
        //     {
        //         Assert.Contains(newPlayer.Id, players.Select(_ => _.Id));
        //     }
        // }

        [Fact]
        public async Task GameTestEndRound()
        {
            var seedDatabase = new SeedDatabase(_connectionString);
            seedDatabase.Run();

            var gameId = await PlayGameUntilEndRound();

            var game = await _shmearService.GetGame(gameId);
            var roundResult = await _shmearService.EndRound(game.Id);
            Assert.True(roundResult.Team1RoundChange == 5 || roundResult.Team1RoundChange == -5);
        }

        private async Task<int> PlayGameUntilEndRound()
        {
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

            // Set wager 5 so that we can guess the resulting end points to be either 5 or -5
            var gamePlayerNextWager = await _shmearService.GetNextWagerGamePlayer(game.Id);
            await _shmearService.SetWager(game.Id, gamePlayerNextWager.PlayerId, 5);
            
            var board = await _shmearService.GetBoardByGameId(game.Id);

            for (int cardsPlayedCount = 0; cardsPlayedCount < 6; cardsPlayedCount++)
            {
                var trick = (await _shmearService.GetTricks(game.Id))
                    .SingleOrDefault(_ => _.CompletedDate == null);
                if (trick == null)
                    trick = await _shmearService.CreateTrick(game.Id);
                
                for (int playersPlayedCount = 0; playersPlayedCount < 4; playersPlayedCount++)
                {
                    var gamePlayerNextCard = await _shmearService.GetNextCardGamePlayer(game.Id, trick.Id);
                    if (cardsPlayedCount == 0 && playersPlayedCount == 0)
                    {
                        var firstCard = await _playerComputerService.PlayCard(game.Id, gamePlayerNextCard.PlayerId);
                        trick = await _shmearService.PlayCard(trick.Id, gamePlayerNextCard.PlayerId, firstCard);
                        continue;
                    }

                    var handCards = await _shmearService.GetHand(game.Id, gamePlayerNextCard.PlayerId);
                    foreach (var handCard in handCards)
                    {
                        if (await _shmearService.ValidCardPlay(game.Id, board.Id, gamePlayerNextCard.PlayerId, handCard.CardId))
                        {
                            trick = await _shmearService.PlayCard(trick.Id, gamePlayerNextCard.PlayerId, handCard.CardId);
                            break;
                        }
                    }

                }
                await _shmearService.EndTrick(trick.Id);
            }

            return game.Id;
        }
    }
}
