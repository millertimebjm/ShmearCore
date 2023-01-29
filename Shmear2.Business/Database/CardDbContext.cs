using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Shmear2.Business.Database.Models;
using Shmear2.Business.Configuration;

namespace Shmear2.Business.Database;

public class CardDbContext : DbContext
{
    public DbSet<Board> Board { get; set; }
    public DbSet<Card> Card { get; set; }
    public DbSet<Game> Game { get; set; }
    public DbSet<GamePlayer> GamePlayer { get; set; }
    public DbSet<HandCard> HandCard { get; set; }
    public DbSet<Player> Player { get; set; }
    public DbSet<PlayerComputer> PlayerComputer { get; set; }
    public DbSet<Suit> Suit { get; set; }
    public DbSet<Trick> Trick { get; set; }
    public DbSet<TrickCard> TrickCard { get; set; }
    public DbSet<Value> Value { get; set; }

    private readonly IConfigurationService _configurationService;
    public CardDbContext(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(_configurationService.GetInMemoryDatabaseConnectionString());
    }
}