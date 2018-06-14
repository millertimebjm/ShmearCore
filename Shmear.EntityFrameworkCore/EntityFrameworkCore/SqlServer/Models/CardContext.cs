using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models
{
    public partial class CardContext : DbContext
    {
        public virtual DbSet<Board> Board { get; set; }
        public virtual DbSet<Card> Card { get; set; }
        public virtual DbSet<Game> Game { get; set; }
        public virtual DbSet<GamePlayer> GamePlayer { get; set; }
        public virtual DbSet<HandCard> HandCard { get; set; }
        public virtual DbSet<Player> Player { get; set; }
        public virtual DbSet<Suit> Suit { get; set; }
        public virtual DbSet<Trick> Trick { get; set; }
        public virtual DbSet<TrickCard> TrickCard { get; set; }
        public virtual DbSet<Value> Value { get; set; }

        public CardContext()
            : base()
        {

        }

        public CardContext(DbContextOptions<CardContext> options)
            : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer(@"Server=localhost;Database=Card.Dev;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Board>(entity =>
            {
                entity.Property(e => e.DateTime)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.DealerPlayer)
                    .WithMany(p => p.Board)
                    .HasForeignKey(d => d.DealerPlayerId)
                    .HasConstraintName("FK_Board_Player");

                entity.HasOne(d => d.Game)
                    .WithMany(p => p.Board)
                    .HasForeignKey(d => d.GameId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Board_Game");

                entity.HasOne(d => d.TrumpSuit)
                    .WithMany(p => p.Board)
                    .HasForeignKey(d => d.TrumpSuitId)
                    .HasConstraintName("FK_Board_Suit");
            });

            modelBuilder.Entity<Card>(entity =>
            {
                entity.HasOne(d => d.Suit)
                    .WithMany(p => p.Card)
                    .HasForeignKey(d => d.SuitId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Card_Suit");

                entity.HasOne(d => d.Value)
                    .WithMany(p => p.Card)
                    .HasForeignKey(d => d.ValueId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Card_Value");
            });

            modelBuilder.Entity<Game>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.Property(e => e.StartedDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<GamePlayer>(entity =>
            {
                entity.HasOne(d => d.Game)
                    .WithMany(p => p.GamePlayer)
                    .HasForeignKey(d => d.GameId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GamePlayer_Game");

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.GamePlayer)
                    .HasForeignKey(d => d.PlayerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_GamePlayer_Player");
            });

            modelBuilder.Entity<HandCard>(entity =>
            {
                entity.HasOne(d => d.Card)
                    .WithMany(p => p.HandCard)
                    .HasForeignKey(d => d.CardId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HandCard_Card");

                entity.HasOne(d => d.Game)
                    .WithMany(p => p.HandCard)
                    .HasForeignKey(d => d.GameId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HandCard_Game");

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.HandCard)
                    .HasForeignKey(d => d.PlayerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HandCard_Player");
            });

            modelBuilder.Entity<Player>(entity =>
            {
                entity.Property(e => e.ConnectionId)
                    .IsRequired()
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.KeepAlive).HasColumnType("datetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Suit>(entity =>
            {
                entity.Property(e => e.Char)
                    .IsRequired()
                    .HasMaxLength(1);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Trick>(entity =>
            {
                entity.Property(e => e.CompletedDate).HasColumnType("datetime");

                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.HasOne(d => d.Game)
                    .WithMany(p => p.Trick)
                    .HasForeignKey(d => d.GameId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Trick_Game");

                entity.HasOne(d => d.WinningPlayer)
                    .WithMany(p => p.Trick)
                    .HasForeignKey(d => d.WinningPlayerId)
                    .HasConstraintName("FK_Trick_Player");
            });

            modelBuilder.Entity<TrickCard>(entity =>
            {
                entity.HasOne(d => d.Card)
                    .WithMany(p => p.TrickCard)
                    .HasForeignKey(d => d.CardId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TrickCard_Card");

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.TrickCard)
                    .HasForeignKey(d => d.PlayerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TrickCard_Player");

                entity.HasOne(d => d.Trick)
                    .WithMany(p => p.TrickCard)
                    .HasForeignKey(d => d.TrickId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TrickCard_Trick");
            });

            modelBuilder.Entity<Value>(entity =>
            {
                entity.Property(e => e.Char)
                    .IsRequired()
                    .HasMaxLength(1);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);
            });
        }
    }
}
