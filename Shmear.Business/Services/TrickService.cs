using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shmear.Business.Services
{
    public static class TrickService
    {
        public static async Task<IEnumerable<Trick>> GetTricks(DbContextOptions<CardContext> options, int gameId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.Trick.Where(_ => _.GameId == gameId).ToListAsync();
            }
        }

        public static async Task<Trick> GetTrick(DbContextOptions<CardContext> options, int trickId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await GetTrick(db, trickId);
            }
        }

        public static async Task<Trick> GetTrick(CardContext db, int trickId)
        {
            return await db.Trick.SingleAsync(_ => _.Id == trickId);
        }

        public static async Task<Trick> CreateTrick(DbContextOptions<CardContext> options, int gameId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var trick = new Trick()
                {
                    GameId = gameId,
                    CreatedDate = DateTime.Now,
                    Sequence = (await db.Trick.CountAsync(_ => _.GameId == gameId)) + 1,
                    CompletedDate = null
                };
                await db.Trick.AddAsync(trick);
                await db.SaveChangesAsync();
                return await GetTrick(options, trick.Id);
            }
        }

        public static async Task<Trick> EndTrick(DbContextOptions<CardContext> options, int trickId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var trick = await GetTrick(db, trickId);
                trick.CompletedDate = DateTime.Now;
                var trickCards = await GetTrickCards(options, trickId);
                trick.WinningPlayerId = BoardService.DetermineWinningPlayerId(options, trick.GameId, trickCards);
                await db.SaveChangesAsync();

                return await GetTrick(options, trick.Id);
            }
        }

        public static async Task<Trick> PlayCard(DbContextOptions<CardContext> options, int trickId, int playerId, int cardId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var highestSequence = ((await db.TrickCard.Where(_ => _.TrickId == trickId).OrderByDescending(_ => _.Sequence).FirstOrDefaultAsync()) ?? new TrickCard()).Sequence;
                var trickCard = new TrickCard()
                {
                    CardId = cardId,
                    PlayerId = playerId,
                    Sequence = highestSequence + 1,
                    TrickId = trickId
                };
                await db.TrickCard.AddAsync(trickCard);
                await db.SaveChangesAsync();
            }

            var trick = await GetTrick(options, trickId);

            using (var db = CardContextFactory.Create(options))
            {
                HandCard handCard = db.HandCard.Single(_ => _.GameId == trick.GameId && _.PlayerId == playerId && _.CardId == cardId);
                db.HandCard.Remove(handCard);

                var gameId = trick.GameId;
                var board = await db.Board.SingleAsync(_ => _.GameId == gameId);
                if (board.TrumpSuitId == null || board.TrumpSuitId == 0)
                    board.TrumpSuitId = db.Card.Single(_ => _.Id == cardId).SuitId;

                db.SaveChanges();
                return trick;
            }
        }

        internal async static Task<IEnumerable<TrickCard>> GetAllTrickCards(DbContextOptions<CardContext> options, int gameId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.TrickCard.Include(tc => tc.Card).ThenInclude(c => c.Suit).Include(tc => tc.Card).ThenInclude(c => c.Value).Where(_ => _.Trick.GameId == gameId).ToListAsync();
            }
        }

        public static void ClearTricks(DbContextOptions<CardContext> options, int gameId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var tricks = db.Trick.Where(_ => _.GameId == gameId);
                foreach (var trick in tricks)
                {
                    var trickCards = db.TrickCard.Where(_ => _.TrickId == trick.Id);
                    db.TrickCard.RemoveRange(trickCards);
                }
                db.Trick.RemoveRange(tricks);
                db.SaveChanges();
            }
        }

        public async static Task<IEnumerable<TrickCard>> GetTrickCards(DbContextOptions<CardContext> options, int trickId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.TrickCard.Where(_ => _.TrickId == trickId).ToListAsync();
            }
        }
    }
}
