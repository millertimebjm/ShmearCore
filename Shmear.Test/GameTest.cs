using Microsoft.EntityFrameworkCore;
using Shmear.Business.Services;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Shmear.Test
{
    public class GameTest : BaseShmearTest
    {
        public GameTest() : base()
        {

        }

        [Fact]
        public async void GameTestNew()
        {
            var game = await GameService.CreateGame(options);
            Assert.True(game.Id > 0);
            Assert.True(game.CreatedDate != null && game.CreatedDate > DateTime.Now.AddMinutes(-1) && game.CreatedDate < DateTime.Now);
            Assert.True(game.StartedDate == null);
            Assert.True(game.Team1Points == 0);
            Assert.True(game.Team2Points == 0);
        }

        [Fact]
        public async void GameTestOpen()
        {
            var game = await GameService.CreateGame(options);
            var openGame = await GameService.GetOpenGame(options);
            Assert.True(openGame.StartedDate == null);
        }

        [Fact]
        public async void GameTestGet()
        {
            var game = await GameService.CreateGame(options);
            var newGame = await GameService.GetGame(options, game.Id);
            Assert.True(game.Id == newGame.Id);
        }

        [Fact]
        public async void GameTestAddPlayer()
        {
            var game = await GameService.CreateGame(options);
            var player = GenerateNewPlayer($"GameTestAddPlayer");
            player = await PlayerService.SavePlayer(options, player);
            var gameAddPlayerChange = await GameService.AddPlayer(options, game.Id, player.Id, 0);

            // Player Keepalive and Game Player Add is two different records being changed
            Assert.True(gameAddPlayerChange == 2);
        }

        [Fact]
        public async void GameTestGetGamePlayer()
        {
            var game = await GameService.CreateGame(options);
            var player = GenerateNewPlayer($"GameTestGetGamePlayer");
            player = await PlayerService.SavePlayer(options, player);
            var gameAddPlayerChange = await GameService.AddPlayer(options, game.Id, player.Id, 0);
            var newPlayer = await GameService.GetGamePlayer(options, game.Id, player.Id);
            Assert.True(newPlayer.PlayerId == player.Id);
        }

        [Fact]
        public async void GameTestGetGamePlayers()
        {
            var game = await GameService.CreateGame(options);
            var players = new List<Player>();
            for (int i = 0; i < 4; i++)
            {
                var player = GenerateNewPlayer($"GameTestGetGamePlayers{i}");
                player = await PlayerService.SavePlayer(options, player);
                var gameAddPlayerChange = await GameService.AddPlayer(options, game.Id, player.Id, i);
                players.Add(player);
            }

            var gamePlayers = await GameService.GetGamePlayers(options, game.Id);
            Assert.True(gamePlayers.Count() == 4);
            foreach (var gamePlayer in gamePlayers)
            {
                Assert.Contains(gamePlayer.PlayerId, players.Select(_ => _.Id));
            }
        }

        [Fact]
        public async void GameTestGetPlayersByGame()
        {
            var game = await GameService.CreateGame(options);
            var players = new List<Player>();
            for (int i = 0; i < 4; i++)
            {
                var player = GenerateNewPlayer($"GameTestGetGamePlayers{i}");
                player = await PlayerService.SavePlayer(options, player);
                var gameAddPlayerChange = await GameService.AddPlayer(options, game.Id, player.Id, i);
                players.Add(player);
            }

            var newPlayers = await GameService.GetPlayersByGameAsync(options, game.Id);
            Assert.True(newPlayers.Count() == 4);
            foreach (var newPlayer in newPlayers)
            {
                Assert.Contains(newPlayer.Id, players.Select(_ => _.Id));
            }
        }

        [Fact]
        public async void GameTestEndRound()
        {
            var seedDatabase = new SeedDatabase();
            seedDatabase.RunWithOptions(options);

            var game = await GameService.CreateGame(options);
            var players = new List<Player>();
            for (int i = 0; i < 4; i++)
            {
                var player = GenerateNewPlayer($"PlayerComputerTestWagerBest{i}");
                await PlayerService.SavePlayer(options, player);
                await GameService.AddPlayer(options, game.Id, player.Id, i);
                var gamePlayer = await GameService.GetGamePlayer(options, game.Id, player.Id);
                gamePlayer.Ready = true;
                await GameService.SaveGamePlayer(options, gamePlayer);
                players.Add(player);
            }
            await GameService.StartGame(options, game.Id);
            await BoardService.StartRound(options, game.Id);
            await BoardService.DealCards(options, game.Id);
            
            // Set wager 5 so that we can guess the resulting end points to be either 5 or -5
            var gamePlayerNextWager = await BoardService.GetNextWagerPlayer(options, game.Id);
            await BoardService.SetWager(options, game.Id, gamePlayerNextWager.PlayerId, 5);
            var board = await BoardService.GetBoardByGameId(options, game.Id);

            for (int cardsPlayedCount = 0; cardsPlayedCount < 6; cardsPlayedCount++)
            {
                var trick = await TrickService.CreateTrick(options, game.Id);
                for (int playersPlayedCount = 0; playersPlayedCount < 4; playersPlayedCount++)
                {
                    var gamePlayerNextCard = await BoardService.GetNextCardPlayer(options, game.Id, trick.Id);
                    if (cardsPlayedCount == 0 && playersPlayedCount == 0)
                    {
                        var firstCard = await PlayerComputerService.PlayCard(options, game.Id, gamePlayerNextCard.PlayerId);
                        trick = await TrickService.PlayCard(options, trick.Id, gamePlayerNextCard.PlayerId, firstCard.Id);
                        continue;
                    }

                    var handCards = await HandService.GetHand(options, game.Id, gamePlayerNextCard.PlayerId);
                    foreach (var handCard in handCards)
                    {
                        if (await GameService.ValidCardPlay(options, game.Id, board.Id, gamePlayerNextCard.PlayerId, handCard.CardId))
                        {
                            trick = await TrickService.PlayCard(options, trick.Id, gamePlayerNextCard.PlayerId, handCard.CardId);
                            break;
                        }
                    }
                    
                }
                await TrickService.EndTrick(options, trick.Id);
            }


            var roundResult = await BoardService.EndRound(options, game.Id);
            Assert.True(roundResult.Team1RoundChange == 5 || roundResult.Team1RoundChange == -5);
        }

        [Fact]
        public void GameTestEfCoreError()
        {
            using (var db = CardContextFactory.Create(options))
            {
                //var seedDatabase = new SeedDatabase();
                //seedDatabase.RunWithOptions(options);

                var card = new Card();
                db.Card.Add(card);
                db.SaveChanges();
                var game = new Game();
                db.Game.Add(game);
                var player = new Player();
                db.Player.Add(player);
                var gamePlayer = new GamePlayer()
                {
                    GameId = game.Id
                };
                db.GamePlayer.Add(gamePlayer);
                var trick = new Trick()
                {
                    GameId = game.Id,
                };
                db.Trick.Add(trick);
                var trickCard = new TrickCard()
                {
                    PlayerId = player.Id,                    
                    TrickId = trick.Id,
                    CardId = db.Card.First().Id,
                };
                db.TrickCard.Add(trickCard);
                db.SaveChanges();
                Assert.Throws<KeyNotFoundException>(() => db.Trick.Include(_ => _.TrickCard).First());
                var trick2 = db.Trick.Include(tc => tc.TrickCard).First();
                
            }

            
        }
    }
}
