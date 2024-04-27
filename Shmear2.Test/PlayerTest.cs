namespace Shmear2.Test;
using Shmear2.Business.Services;
using Business.Database.Models;

public class PlayerTest : BaseShmearTest
{
    [Fact]
    public async Task PlayerTestSave()
    {
        var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
        IPlayerService playerService = new PlayerService(cardDbContext);
        var player = GenerateNewPlayer("PlayerTestSave");
        player = await playerService.SavePlayer(player);
        Assert.True(player.Id > 0);
    }

    [Fact]
    public async Task PlayerTestGet()
    {
        var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
        IPlayerService playerService = new PlayerService(cardDbContext);
        var player = GenerateNewPlayer("PlayerTestGet");
        player = await playerService.SavePlayer(player);

        var playerById = await playerService.GetPlayer(player.Id);
        Assert.True(playerById.Id == player.Id);
        Assert.True(playerById.ConnectionId == player.ConnectionId);
        Assert.True(playerById.Name == player.Name);
        Assert.True(playerById.KeepAlive == player.KeepAlive);

        var playerByName = await playerService.GetPlayerByName(player.Name);
        Assert.True(playerByName.Id == player.Id);
        Assert.True(playerByName.ConnectionId == player.ConnectionId);
        Assert.True(playerByName.Name == player.Name);
        Assert.True(playerByName.KeepAlive == player.KeepAlive);
    }

    [Fact]
    public async Task PlayerTestDelete()
    {
        var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
        IPlayerService playerService = new PlayerService(cardDbContext);
        var player = GenerateNewPlayer("PlayerTestDelete");
        player = await playerService.SavePlayer(player);

        var changeCount = await playerService.DeletePlayer(player.Id);
        Assert.True(changeCount > 0);

        var deletedPlayerById = await playerService.GetPlayer(player.Id);
        Assert.True(deletedPlayerById == null);

        var deletedPlayerByName = await playerService.GetPlayerByName(player.Name);
        Assert.True(deletedPlayerByName == null);
    }

    [Fact]
    public async Task GetPlayer_Equal()
    {
        var cardDbContext = GenerateCardDbContext(Guid.NewGuid().ToString());
        IPlayerService playerService = new PlayerService(cardDbContext);
        DateTime dt = DateTime.UtcNow;
        var player = new Player()
        {
            Id = 0,
            Name = GenerateRandomString(20),
            ConnectionId = Guid.NewGuid().ToString(),
            KeepAlive = dt,
        };
        player = await playerService.SavePlayer(player);

        var getPlayer = await playerService.GetPlayer(player.Id);
        Assert.Equal(player.Id, getPlayer.Id);
        Assert.Equal(player.ConnectionId, getPlayer.ConnectionId);
        Assert.Equal(player.Name, getPlayer.Name);
        Assert.Equal(player.KeepAlive, getPlayer.KeepAlive);
    }
}
