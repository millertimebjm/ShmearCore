﻿using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shmear.Business.Services
{
    public static class HandService
    {
        public static async Task<bool> AddCard(DbContextOptions<CardContext> options, int gameId, int playerId, int cardId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var handCard = new HandCard() { PlayerId = playerId, CardId = cardId, GameId = gameId };
                await db.HandCard.AddAsync(handCard);
                await db.SaveChangesAsync();
            }
            return true;
        }

        public static async Task<IEnumerable<HandCard>> GetHand(DbContextOptions<CardContext> options, int gameId, int playerId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.HandCard.Include(hc => hc.Card).ThenInclude(c => c.Suit).Include(hc => hc.Card).ThenInclude(c => c.Value).Where(_ => _.GameId == gameId && _.PlayerId == playerId).ToListAsync();
            }
        }
    }
}
