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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // base.OnModelCreating(modelBuilder);
        // modelBuilder.Entity<Game>()
        //     .HasMany(_ => _.Trick)
        //     .WithOne(_ => _.Game)
        //     .HasForeignKey(_ => _.GameId);

        // modelBuilder.Entity<Game>()
        //     .HasMany(_ => _.GamePlayer)
        //     .WithOne(_ => _.Game)
        //     .HasForeignKey(_ => _.GameId);

        // modelBuilder.Entity<Player>()
        //     .HasOne(_ => _.GamePlayer)
        //     .WithOne();

        // modelBuilder.Entity<Card>()
        //     .HasOne(_ => _.Suit)
        //     .WithOne();

        // modelBuilder.Entity<Card>()
        //     .HasOne(_ => _.Value)
        //     .WithOne();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(_configurationService.GetInMemoryDatabaseConnectionString());
    }
}