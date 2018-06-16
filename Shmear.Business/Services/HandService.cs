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
    public class HandService
    {
        public static async Task<bool> AddCard(DbContextOptions<CardContext> options, int gameId, int playerId, int cardId)
        {
            using (var db = CardFactory.Create(options))
            {
                var handCard = new HandCard() { PlayerId = playerId, CardId = cardId, GameId = gameId };
                await db.HandCard.AddAsync(handCard);
                await db.SaveChangesAsync();
            }
            return true;
        }

        public static async Task<IEnumerable<HandCard>> GetHand(DbContextOptions<CardContext> options, int gameId, int playerId)
        {
            using (var db = CardFactory.Create(options))
            {
                return await db.HandCard.Include(_ => _.Card).ThenInclude(_ => _.Suit).Where(_ => _.GameId == gameId && _.PlayerId == playerId).ToListAsync();
            }
        }

        //public static bool RemoveCard(int gameId, int playerId, int cardId)
        //{
        //    using (var db = new ShmearDataContext())
        //    {
        //        var handCard = db.HandCards.Single(_ => _.PlayerId == playerId && _.CardId == cardId);
        //        db.HandCards.DeleteOnSubmit(handCard);
        //        db.SubmitChanges();
        //    }

        //    return true;
        //}
    }
}
