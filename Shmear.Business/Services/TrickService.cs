using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shmear.Business.Services
{
    public class TrickService
    {
        public static async Task<IEnumerable<Trick>> GetTricks(int gameId)
        {
            using (var db = new CardContext())
            {
                return await db.Trick.Include(_ => _.TrickCard).Where(_ => _.GameId == gameId).ToListAsync();
            }
        }

        public static async Task<Trick> GetTrick(int trickId)
        {
            using (var db = new CardContext())
            {
                return await db.Trick.Include(_ => _.TrickCard).SingleAsync(_ => _.Id == trickId);
            }
        }

        public static async Task<Trick> CreateTrick(int gameId)
        {
            using (var db = new CardContext())
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
                return await GetTrick(trick.Id);
            }
        }

        //public static Trick EndTrick(int trickId)
        //{
        //    using (var db = new ShmearDataContext())
        //    {
        //        var trick = db.Tricks.Single(_ => _.Id == trickId);
        //        trick.CompletedDate = DateTime.Now;
        //        trick.WinningPlayerId = BoardService.DetermineWinningPlayerId(trick.GameId, trick.TrickCards);
        //        db.SubmitChanges();

        //        return GetTrick(trick.Id);
        //    }
        //}

        //public static Trick PlayCard(int trickId, int playerId, int cardId)
        //{
        //    using (var db = new ShmearDataContext())
        //    {
        //        var highestSequence = (db.TrickCards.Where(_ => _.TrickId == trickId).OrderByDescending(_ => _.Sequence).FirstOrDefault() ?? new TrickCard()).Sequence;
        //        var trickCard = new TrickCard()
        //        {
        //            CardId = cardId,
        //            PlayerId = playerId,
        //            Sequence = highestSequence + 1,
        //            TrickId = trickId
        //        };
        //        db.TrickCards.InsertOnSubmit(trickCard);

        //        var trick = db.Tricks.Single(_ => _.Id == trickId);
        //        db.HandCards.DeleteOnSubmit(db.HandCards.Single(_ => _.GameId == trick.GameId && _.PlayerId == playerId && _.CardId == cardId));

        //        var gameId = trick.GameId;
        //        var board = db.Boards.Single(_ => _.GameId == gameId);
        //        if (board.TrumpSuitId == null || board.TrumpSuitId == 0)
        //            board.TrumpSuitId = db.Cards.Single(_ => _.Id == cardId).SuitId;

        //        db.SubmitChanges();
        //        return GetTrick(trick.Id);
        //    }
        //}

        //public static void ClearTricks(int gameId)
        //{
        //    using (var db = new ShmearDataContext())
        //    {
        //        var tricks = db.Tricks.Where(_ => _.GameId == gameId);
        //        foreach (var trick in tricks)
        //        {
        //            var trickCards = db.TrickCards.Where(_ => _.TrickId == trick.Id);
        //            db.TrickCards.DeleteAllOnSubmit(trickCards);
        //        }
        //        db.Tricks.DeleteAllOnSubmit(tricks);
        //        db.SubmitChanges();
        //    }
        //}
    }
}
