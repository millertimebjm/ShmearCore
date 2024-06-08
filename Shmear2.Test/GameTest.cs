using Shmear2.Business.Services;
using Shmear2.Business.Database.Models;
using Shmear2.Business.Database;

namespace Shmear2.Test
{
    public class GameTest : BaseShmearTest
    {
        [Fact]
        public async Task CreateGame()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            var game = await shmearService.CreateGame();
            Assert.True(game.Id > 0);
            Assert.True(game.CreatedDate > DateTime.Now.AddMinutes(-1) && game.CreatedDate < DateTime.Now);
            Assert.True(game.StartedDate == null);
            Assert.True(game.Team1Points == 0);
            Assert.True(game.Team2Points == 0);
        }

        [Fact]
        public async Task GetOpenGame()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            var game = await shmearService.CreateGame();
            var openGame = await shmearService.GetOpenGame();
            Assert.True(openGame.StartedDate == null);
        }

        [Fact]
        public async Task GetGame()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            var game = await shmearService.CreateGame();
            var newGame = await shmearService.GetGame(game.Id);
            Assert.True(game.Id == newGame.Id);
        }

        [Fact]
        public async Task AddPlayer()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            IPlayerService playerService = new PlayerService(cardDbContext);
            var game = await shmearService.CreateGame();
            var player = GenerateNewPlayer($"GameTestAddPlayer");
            player = await playerService.SavePlayer(player);
            var gameAddPlayerChange = await shmearService.AddPlayer(game.Id, player.Id, 0);

            // Player Keepalive and Game Player Add is two different records being changed
            Assert.True(gameAddPlayerChange == 2);
        }

        [Fact]
        public async Task GetGamePlayer()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            IPlayerService playerService = new PlayerService(cardDbContext);
            var game = await shmearService.CreateGame();
            var player = GenerateNewPlayer($"GameTestGetGamePlayer");
            player = await playerService.SavePlayer(player);
            await shmearService.AddPlayer(game.Id, player.Id, 0);

            var newPlayer = await shmearService.GetGamePlayer(game.Id, player.Id);
            Assert.True(newPlayer.PlayerId == player.Id);

            var newPlayer2 = await shmearService.GetGamePlayer(newPlayer.Id);
            Assert.True(newPlayer2.PlayerId == player.Id);

            var newPlayer3 = await shmearService.GetGamePlayer(game.Id, player.ConnectionId);
            Assert.True(newPlayer3.PlayerId == player.Id);
        }

        [Fact]
        public async Task GetGamePlayers()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            IPlayerService playerService = new PlayerService(cardDbContext);
            var game = await shmearService.CreateGame();
            var players = new List<Player>();
            for (int i = 0; i < 4; i++)
            {
                var player = GenerateNewPlayer($"GameTestGetGamePlayers{i}");
                player = await playerService.SavePlayer(player);
                await shmearService.AddPlayer(game.Id, player.Id, i);
                players.Add(player);
            }

            var gamePlayers = await shmearService.GetGamePlayers(game.Id);
            Assert.True(gamePlayers.Count() == 4);
            foreach (var gamePlayer in gamePlayers)
            {
                Assert.Contains(gamePlayer.PlayerId, players.Select(_ => _.Id));
            }
        }

        [Fact]
        public async Task GetPlayersByGameAsync()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            IPlayerService playerService = new PlayerService(cardDbContext);
            var game = await shmearService.CreateGame();
            var players = new List<Player>();
            for (int i = 0; i < 4; i++)
            {
                var player = GenerateNewPlayer($"GameTestGetGamePlayers{i}");
                player = await playerService.SavePlayer(player);
                await shmearService.AddPlayer(game.Id, player.Id, i);
                players.Add(player);
            }

            var newPlayers = await shmearService.GetPlayersByGameAsync(game.Id);
            Assert.True(newPlayers.Count() == 4);
            foreach (var newPlayer in newPlayers)
            {
                Assert.Contains(newPlayer.Id, players.Select(_ => _.Id));
            }
        }

        [Fact]
        public async Task EndRound()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            shmearService.SeedValues();
            shmearService.SeedSuits();
            shmearService.SeedCards();


            var gameId = await PlayGameUntilEndRound(
                cardDbContext,
                shmearService
            );

            var game = await shmearService.GetGame(gameId);
            var roundResult = await shmearService.EndRound(game.Id);
            Assert.True(roundResult.Team1RoundChange == 5 || roundResult.Team1RoundChange == -5);
        }

        [Fact]
        public async Task GetHumanGamePlayers()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            IPlayerService playerService = new PlayerService(cardDbContext);
            var game = await shmearService.CreateGame();

            var player = GenerateNewPlayer("GetHumanGamePlayers{0}");
            player = await playerService.SavePlayer(player);
            await shmearService.AddPlayer(game.Id, player.Id, 0);

            var computerPlayer = GenerateNewComputerPlayer("GetHumanGamePlayers{1}");
            computerPlayer = await playerService.SavePlayer(computerPlayer);
            await shmearService.AddPlayer(game.Id, computerPlayer.Id, 1);

            var allGamePlayers = await shmearService.GetGamePlayers(game.Id);
            Assert.Equal(2, allGamePlayers.Count());

            var humanGamePlayers = await shmearService.GetHumanGamePlayers(game.Id);
            Assert.Single(humanGamePlayers);
            Assert.Equal(humanGamePlayers.Single().PlayerId, player.Id);
        }

        public async Task RemovePlayer()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            IPlayerService playerService = new PlayerService(cardDbContext);
            var game = await shmearService.CreateGame();

            var player1 = GenerateNewPlayer("RemovePlayer{0}");
            player1 = await playerService.SavePlayer(player1);
            await shmearService.AddPlayer(game.Id, player1.Id, 0);

            var computerPlayer1 = GenerateNewComputerPlayer("RemovePlayer{1}");
            computerPlayer1 = await playerService.SavePlayer(computerPlayer1);
            await shmearService.AddPlayer(game.Id, computerPlayer1.Id, 1);

            var player2 = GenerateNewPlayer("RemovePlayer{2}");
            player2 = await playerService.SavePlayer(player2);
            await shmearService.AddPlayer(game.Id, player2.Id, 2);

            var computerPlayer2 = GenerateNewComputerPlayer("RemovePlayer{3}");
            computerPlayer2 = await playerService.SavePlayer(computerPlayer2);
            await shmearService.AddPlayer(game.Id, computerPlayer2.Id, 3);

            var allGamePlayers = await shmearService.GetGamePlayers(game.Id);
            Assert.Equal(4, allGamePlayers.Count());

            await shmearService.RemovePlayer(game.Id, player2.Id);
            await shmearService.RemovePlayer(game.Id, computerPlayer1.Id);

            var allGamePlayersAfterRemove = await shmearService.GetGamePlayers(game.Id);
            Assert.Equal(2, allGamePlayersAfterRemove.Count());
            Assert.True(allGamePlayersAfterRemove.Single(_ => _.PlayerId == player1.Id).PlayerId == player1.Id);
            Assert.True(allGamePlayersAfterRemove.Single(_ => _.PlayerId == computerPlayer2.Id).PlayerId == computerPlayer2.Id);
        }

        //[Fact] Not ready for testing
        private async Task ValidCardPlay_FirstCard()
        {
            var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
            IShmearService shmearService = new ShmearService(cardDbContext);
            await shmearService.SeedValues();
            await shmearService.SeedSuits();
            await shmearService.SeedCards();
        }

        private async Task<int> PlayGameUntilEndRound(
            CardDbContext cardDbContext,
            IShmearService shmearService)
        {
            IPlayerService playerService = new PlayerService(cardDbContext);
            IPlayerComputerService playerComputerService = new PlayerComputerService(
                cardDbContext,
                shmearService,
                playerService
            );

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

            // Set wager 5 so that we can guess the resulting end points to be either 5 or -5
            var gamePlayerNextWager = await shmearService.GetNextWagerGamePlayer(game.Id);
            await shmearService.SetWager(game.Id, gamePlayerNextWager.PlayerId, 5);
            
            var board = await shmearService.GetBoardByGameId(game.Id);

            for (int cardsPlayedCount = 0; cardsPlayedCount < 6; cardsPlayedCount++)
            {
                var trick = (await shmearService.GetTricks(game.Id))
                    .SingleOrDefault(_ => _.CompletedDate == null);
                if (trick == null)
                    trick = await shmearService.CreateTrick(game.Id);
                
                for (int playersPlayedCount = 0; playersPlayedCount < 4; playersPlayedCount++)
                {
                    var gamePlayerNextCard = await shmearService.GetNextCardGamePlayer(game.Id, trick.Id);
                    if (cardsPlayedCount == 0 && playersPlayedCount == 0)
                    {
                        var firstCard = await playerComputerService.PlayCard(game.Id, gamePlayerNextCard.PlayerId);
                        trick = await shmearService.PlayCard(trick.Id, gamePlayerNextCard.PlayerId, firstCard);
                        continue;
                    }

                    var handCards = await shmearService.GetHand(game.Id, gamePlayerNextCard.PlayerId);

                    foreach (var handCard in handCards)
                    {
                        if (await shmearService.ValidCardPlay(game.Id, board.Id, gamePlayerNextCard.PlayerId, handCard.CardId))
                        {
                            trick = await shmearService.PlayCard(trick.Id, gamePlayerNextCard.PlayerId, handCard.CardId);
                            break;
                        }
                    }

                }
                await shmearService.EndTrick(trick.Id);
            }

            return game.Id;
        }
    }
}
