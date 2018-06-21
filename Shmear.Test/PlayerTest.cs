using Shmear.Business.Services;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Shmear.Test
{
    public class PlayerTest : ShmearTest
    {
        private Player _player;
        private string _connectionId = "PlayerTest";

        public PlayerTest() : base()
        {
            _player = new Player()
            {
                ConnectionId = _connectionId,
                Id = 0,
                Name = "PlayerTest",
                KeepAlive = DateTime.Now,
            };
        }

        [Fact]
        public async void PlayerTestSave()
        {
            _player = await PlayerService.SavePlayer(options, _player);
            Assert.True(_player.Id > 0);

            var playerById = await PlayerService.GetPlayer(options, _player.Id);
            Assert.True(playerById.Id == _player.Id);
            Assert.True(playerById.ConnectionId == _player.ConnectionId);
            Assert.True(playerById.Name == _player.Name);
            Assert.True(playerById.KeepAlive == _player.KeepAlive);

            var playerByName = await PlayerService.GetPlayerByName(options, _player.Name);
            Assert.True(playerByName.Id == _player.Id);
            Assert.True(playerByName.ConnectionId == _player.ConnectionId);
            Assert.True(playerByName.Name == _player.Name);
            Assert.True(playerByName.KeepAlive == _player.KeepAlive);
        }
    }
}
