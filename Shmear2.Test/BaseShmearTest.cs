using Shmear2.Business.Configuration;
using Shmear2.Business.Database;
using Shmear2.Business.Services;
using Shmear2.Business.Database.Models;

namespace Shmear2.Test;

public class BaseShmearTest 
{

    protected CardDbContext GenerateCardDbContext(string connectionStringName)
    {
        IConfigurationService configurationService = 
            new ConfigurationService(connectionStringName);
        var cardDbContext = new CardDbContext(configurationService);
        cardDbContext.Database.EnsureCreated();
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

    protected Player GenerateNewComputerPlayer(string name)
    {
        return new Player()
        {
            Id = 0,
            ConnectionId = Guid.NewGuid().ToString(),
            Name = name,
            KeepAlive = DateTime.Now,
            IsComputer = true,
        };
    }

    public static string GenerateRandomString(int length)
    {
        Random random = new Random();
        string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
        string randomString = "";

        for (int i = 0; i < length; i++)
        {
            int index = random.Next(characters.Length);
            randomString += characters[index];
        }

        return randomString;
    }
}
