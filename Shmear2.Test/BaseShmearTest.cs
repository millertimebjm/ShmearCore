using Shmear2.Business.Configuration;
using Shmear2.Business.Database;
using Shmear2.Business.Services;
using Shmear2.Business.Database.Models;

namespace Shmear2.Test;

public class BaseShmearTest 
{
    internal IShmearService _shmearService;
    internal IPlayerService _playerService;
    internal IPlayerComputerService _playerComputerService;

    protected CardDbContext GenerateCardDbContext(string connectionStringName)
    {
        IConfigurationService configurationService = 
            new ConfigurationService(connectionStringName);
        var cardDbContext = new CardDbContext(configurationService);
        return cardDbContext;
    }

    protected Player GenerateNewPlayer(string name)
        {
            return new Player()
            {
                Id = 0,
                ConnectionId = Guid.NewGuid().ToString(),
                Name = name,
                KeepAlive = DateTime.Now,
            };
        }
}