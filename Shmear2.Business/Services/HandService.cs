using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shmear2.Business.Database;
using Shmear2.Business.Models;
using Shmear2.Business.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Shmear2.Business.Services
{
    public class HandService : IHandService
    {
        private readonly CardDbContext _cardDb;
        public HandService(CardDbContext cardDb)
        {
            _cardDb = cardDb;
        }

        public async Task<bool> AddCard(int gameId, int playerId, int cardId)
        {
            var handCard = new HandCard() { PlayerId = playerId, CardId = cardId, GameId = gameId };
            await _cardDb.HandCard.AddAsync(handCard);
            await _cardDb.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<HandCard>> GetHand(int gameId, int playerId)
        {
            return await _cardDb
                .HandCard
                .Include(hc => hc.Card)
                .ThenInclude(c => c.Suit)
                .Include(hc => hc.Card)
                .ThenInclude(c => c.Value)
                .Where(_ => _.GameId == gameId && _.PlayerId == playerId)
                .ToListAsync();
        }
    }
}
