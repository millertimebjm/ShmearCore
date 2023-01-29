
namespace Shmear2.Business.Configuration;

public class ConfigurationService : IConfigurationService
{
    private readonly string _inMemoryDatabaseConnectionString;
    public ConfigurationService(
        string inMemoryDatabaseConnectionString = null
    )
    {
        _inMemoryDatabaseConnectionString = inMemoryDatabaseConnectionString;
    }

    public string GetInMemoryDatabaseConnectionString()
    {
        return "";
    }


    public string GetSqlServerConnectionString()
    {
        return "";
    }
}