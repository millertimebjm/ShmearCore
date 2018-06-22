using Shmear.Business.Services;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Shmear.Test
{
    public class PlayerTest : BaseShmearTest
    {
        public PlayerTest() : base()
        {
            
        }

        [Fact]
        public async void PlayerTestSave()
        {
            var player = GenerateNewPlayer("PlayerTestSave");
            player = await PlayerService.SavePlayer(options, player);
            Assert.True(player.Id > 0);
        }

        [Fact]
        public async void PlayerTestGet()
        {
            var player = GenerateNewPlayer("PlayerTestGet");
            player = await PlayerService.SavePlayer(options, player);

            var playerById = await PlayerService.GetPlayer(options, player.Id);
            Assert.True(playerById.Id == player.Id);
            Assert.True(playerById.ConnectionId == player.ConnectionId);
            Assert.True(playerById.Name == player.Name);
            Assert.True(playerById.KeepAlive == player.KeepAlive);

            var playerByName = await PlayerService.GetPlayerByName(options, player.Name);
            Assert.True(playerByName.Id == player.Id);
            Assert.True(playerByName.ConnectionId == player.ConnectionId);
            Assert.True(playerByName.Name == player.Name);
            Assert.True(playerByName.KeepAlive == player.KeepAlive);
        }

        [Fact]
        public async void PlayerTestDelete()
        {
            var player = GenerateNewPlayer("PlayerTestDelete");
            player = await PlayerService.SavePlayer(options, player);

            var changeCount = await PlayerService.DeletePlayer(options, player.Id);
            Assert.True(changeCount > 0);

            var deletedPlayerById = await PlayerService.GetPlayer(options, player.Id);
            Assert.True(deletedPlayerById == null);

            var deletedPlayerByName = await PlayerService.GetPlayerByName(options, player.Name);
            Assert.True(deletedPlayerByName == null);
        }
    }
}
