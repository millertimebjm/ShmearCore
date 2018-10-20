using Shmear.Business.Services;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Shmear.Test
{
    public class PlayerComputerTest : BaseShmearTest
    {
        public PlayerComputerTest() : base()
        {

        }

        [Fact]
        public async Task PlayerComputerTestWager()
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

            var wager = await PlayerComputerService.Wager(options, game.Id, players.First().Id);
            Assert.True(wager.Value > 0 && wager.Value < 10);
        }

        [Fact]
        public async Task PlayerComputerTestPlayCard()
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

            var trick = await TrickService.CreateTrick(options, game.Id);
            await BoardService.GetNextCardPlayer(options, game.Id, trick.Id);
        }
    }
}
