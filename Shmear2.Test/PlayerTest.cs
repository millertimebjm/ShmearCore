namespace Shmear2.Test;
using Shmear2.Business.Services;

public class PlayerTest : BaseShmearTest
{
    public PlayerTest()
    {
        var cardDbContext = GenerateCardDbContext("PlayerTest");
        _playerService = new PlayerService(cardDbContext);
    }

    [Fact]
    public async Task PlayerTestSave()
    {
        var player = GenerateNewPlayer("PlayerTestSave");
        player = await _playerService.SavePlayer(player);
        Assert.True(player.Id > 0);
    }

    [Fact]
    public async Task PlayerTestGet()
    {
        var player = GenerateNewPlayer("PlayerTestGet");
        player = await _playerService.SavePlayer(player);

        var playerById = await _playerService.GetPlayer(player.Id);
        Assert.True(playerById.Id == player.Id);
        Assert.True(playerById.ConnectionId == player.ConnectionId);
        Assert.True(playerById.Name == player.Name);
        Assert.True(playerById.KeepAlive == player.KeepAlive);

        var playerByName = await _playerService.GetPlayerByName(player.Name);
        Assert.True(playerByName.Id == player.Id);
        Assert.True(playerByName.ConnectionId == player.ConnectionId);
        Assert.True(playerByName.Name == player.Name);
        Assert.True(playerByName.KeepAlive == player.KeepAlive);
    }

    [Fact]
    public async Task PlayerTestDelete()
    {
        var player = GenerateNewPlayer("PlayerTestDelete");
        player = await _playerService.SavePlayer(player);

        var changeCount = await _playerService.DeletePlayer(player.Id);
        Assert.True(changeCount > 0);

        var deletedPlayerById = await _playerService.GetPlayer(player.Id);
        Assert.True(deletedPlayerById == null);

        var deletedPlayerByName = await _playerService.GetPlayerByName(player.Name);
        Assert.True(deletedPlayerByName == null);
    }
}
