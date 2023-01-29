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
    public class TrickService : ITrickService
    {
        private readonly CardDbContext _cardDb;
        private readonly IBoardService _boardService;
        public TrickService(
            CardDbContext cardDb,
            IBoardService boardService)
        {
            _cardDb = cardDb;
            _boardService = boardService;
        }

        public async Task<IEnumerable<Trick>> GetTricks(int gameId)
        {
            return await _cardDb.Trick.Where(_ => _.GameId == gameId).ToListAsync();
        }

        public async Task<Trick> GetTrick(int trickId)
        {
            return await _cardDb.Trick.SingleAsync(_ => _.Id == trickId);
        }

        public async Task<Trick> CreateTrick(int gameId)
        {
            var trick = new Trick()
            {
                GameId = gameId,
                CreatedDate = DateTime.Now,
                Sequence = (await _cardDb.Trick.CountAsync(_ => _.GameId == gameId)) + 1,
                CompletedDate = null
            };
            await _cardDb.Trick.AddAsync(trick);
            await _cardDb.SaveChangesAsync();
            return await GetTrick(trick.Id);
        }

        public async Task<Trick> EndTrick(int trickId)
        {
            var trick = await GetTrick(trickId);
            trick.CompletedDate = DateTime.Now;
            var trickCards = await GetTrickCards(trickId);
            trick.WinningPlayerId = _boardService.DetermineWinningPlayerId(trick.GameId, trickCards);
            await _cardDb.SaveChangesAsync();

            return await GetTrick(trick.Id);
        }

        public async Task<Trick> PlayCard(int trickId, int playerId, int cardId)
        {
            var highestSequence = ((await _cardDb.TrickCard.Where(_ => _.TrickId == trickId).OrderByDescending(_ => _.Sequence).FirstOrDefaultAsync()) ?? new TrickCard()).Sequence;
            var trickCard = new TrickCard()
            {
                CardId = cardId,
                PlayerId = playerId,
                Sequence = highestSequence + 1,
                TrickId = trickId
            };
            await _cardDb.TrickCard.AddAsync(trickCard);
            await _cardDb.SaveChangesAsync();

            var trick = await GetTrick(trickId);

            HandCard handCard = _cardDb
                .HandCard
                .Single(_ => _.GameId == trick.GameId && _.PlayerId == playerId && _.CardId == cardId);
            _cardDb.HandCard.Remove(handCard);

            var gameId = trick.GameId;
            var board = await _cardDb.Board.SingleAsync(_ => _.GameId == gameId);
            if (board.TrumpSuitId == null || board.TrumpSuitId == 0)
                board.TrumpSuitId = _cardDb.Card.Single(_ => _.Id == cardId).SuitId;

            _cardDb.SaveChanges();
            return trick;
        }

        public async Task<IEnumerable<TrickCard>> GetAllTrickCards(int gameId)
        {
            return await _cardDb
                .TrickCard
                .Include(tc => tc.Card)
                .ThenInclude(c => c.Suit)
                .Include(tc => tc.Card)
                .ThenInclude(c => c.Value)
                .Where(_ => _.Trick.GameId == gameId)
                .ToListAsync();
        }

        public void ClearTricks(int gameId)
        {
            var tricks = _cardDb.Trick.Where(_ => _.GameId == gameId);
            foreach (var trick in tricks)
            {
                var trickCards = _cardDb.TrickCard.Where(_ => _.TrickId == trick.Id);
                _cardDb.TrickCard.RemoveRange(trickCards);
            }
            _cardDb.Trick.RemoveRange(tricks);
            _cardDb.SaveChanges();
        }

        public async Task<IEnumerable<TrickCard>> GetTrickCards(int trickId)
        {
            return await _cardDb
                .TrickCard
                .Where(_ => _.TrickId == trickId)
                .ToListAsync();
        }
    }
}
