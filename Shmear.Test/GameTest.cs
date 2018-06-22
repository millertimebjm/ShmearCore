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

        //[Fact]
        //public async void GameTest()
        //{
        //    GameService.SaveGamePlayer
        //}
    }
}
