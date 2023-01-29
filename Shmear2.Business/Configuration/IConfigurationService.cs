
namespace Shmear2.Business.Configuration;

public interface IConfigurationService
{
    string GetInMemoryDatabaseConnectionString();
    string GetSqlServerConnectionString();
}