using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Shmear.Business.Services
{
    public class BoardService
    {
        public async static Task StartRound(int gameId)
        {
            await CreateBoardIfNotExists(gameId);

            await ResetWagers(gameId);
            await ResetTrumpCard(gameId);

            using (var db = new CardContext())
            {
                var board = await BoardService.GetBoardByGameId(gameId);

                var gamePlayers = await db.GamePlayer.Where(_ => _.GameId == gameId).OrderBy(_ => _.SeatNumber).ToListAsync();

                if ((board.DealerPlayerId ?? 0) == 0)
                {
                    board.DealerPlayerId = gamePlayers.First().PlayerId;
                }
                else
                {
                    var dealerPlayerId = (int)board.DealerPlayerId;
                    var playerIndex = gamePlayers.FindIndex(_ => _.PlayerId == dealerPlayerId);
                    var nextDealerIndex = (playerIndex + 1) % 4;
                    board.DealerPlayerId = gamePlayers[nextDealerIndex].PlayerId;
                }
                board = await SaveBoard(board);
            }
        }

        private async static Task ResetWagers(int gameId)
        {
            using (var db = new CardContext())
            {
                var gamePlayers = await db.GamePlayer.Where(_ => _.GameId == gameId).OrderBy(_ => _.SeatNumber).ToListAsync();
                foreach (var gamePlayer in gamePlayers)
                {
                    gamePlayer.Wager = null;
                }
                await db.SaveChangesAsync();
            }
        }

        private static async Task ResetTrumpCard(int gameId)
        {
            using (var db = new CardContext())
            {
                var board = await BoardService.GetBoardByGameId(gameId);
                board.TrumpSuitId = null;
                await BoardService.SaveBoard(board);
            }
        }

        public static async Task<Board> GetBoardByGameId(int gameId)
        {
            using (var db = new CardContext())
            {
                return await db.Board.SingleAsync(_ => _.GameId == gameId);
            }
        }

        private async static Task CreateBoardIfNotExists(int gameId)
        {
            using (var db = new CardContext())
            {
                var board = await db.Board.SingleOrDefaultAsync(_ => _.GameId == gameId);
                if (board == null)
                {
                    board = await SaveBoard(new Board()
                    {
                        GameId = gameId,
                    });
                }
            }
        }

        public async static Task<Board> SaveBoard(Board board)
        {
            using (var db = new CardContext())
            {
                var result = new Board();
                if (board.Id == 0)
                {
                    await db.Board.AddAsync(board);
                    result = board;
                }
                else
                {
                    var boardTemp = await db.Board.SingleAsync(_ => _.Id == board.Id);
                    boardTemp.Team1Wager = board.Team1Wager;
                    boardTemp.Team2Wager = board.Team2Wager;
                    boardTemp.TrumpSuitId = board.TrumpSuitId;
                    boardTemp.DealerPlayerId = board.DealerPlayerId;
                    result = boardTemp;
                }

                await db.SaveChangesAsync();
                return await GetBoard(result.Id);
            }
        }

        public async static Task<Board> GetBoard(int id)
        {
            using (var db = new CardContext())
            {
                return await db.Board.SingleAsync(_ => _.Id == id);
            }
        }

        public static async Task<bool> DealCards(int gameId)
        {
            var cards = (await CardService.GetCards()).Select(_ => _.Id).ToList();

            for (int i = 0; i < (100 + (DateTime.Now.Millisecond % 10)); i++)
            {
                cards = Shuffle(cards);
            }

            var players = (await GameService.GetPlayersByGameAsync(gameId)).ToArray();
            for (var i = 0; i < 24; i++)
            {
                await HandService.AddCard(gameId, players[(i % 4)].Id, cards[i]);
            }

            return true;
        }

        private static List<int> Shuffle(List<int> cards)
        {
            var provider = new RNGCryptoServiceProvider();
            var n = cards.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box); while (!(box[0] < n * (Byte.MaxValue / n)));
                var k = (box[0] % n);
                n--;
                var value = cards[k];
                cards[k] = cards[n];
                cards[n] = value;
            }
            return cards;
        }

        public static async Task<GamePlayer> GetNextWagerPlayer(int gameId)
        {
            var board = await BoardService.GetBoardByGameId(gameId);
            var dealerPlayerId = board.DealerPlayerId;
            var gamePlayers = (await GameService.GetGamePlayers(gameId)).OrderBy(_ => _.SeatNumber).ToList();
            var wagerPlayers = gamePlayers.Count(_ => _.Wager != null);
            var nextGamePlayerIndex = ((gamePlayers.FindIndex(_ => _.PlayerId == dealerPlayerId) + wagerPlayers + 1) % 4);
            var nextPlayer = gamePlayers[nextGamePlayerIndex];
            return nextPlayer;
        }

        public static async Task<GamePlayer> GetNextCardPlayer(int gameId, int trickId)
        {
            var board = await BoardService.GetBoardByGameId(gameId);
            var firstPlayerId = board.DealerPlayerId;
            var gamePlayers = (await GameService.GetGamePlayers(gameId)).OrderBy(_ => _.SeatNumber).ToList();
            var trick = await TrickService.GetTrick(trickId);
            var completedTricks = (await TrickService.GetTricks(gameId)).Where(_ => _.CompletedDate != null);
            int trickStartingPlayer;
            if (completedTricks.Any())
            {
                var latestCompletedTrick = completedTricks.OrderByDescending(_ => _.CompletedDate).First();
                trickStartingPlayer = (int)latestCompletedTrick.WinningPlayerId;
            }
            else
            {
                trickStartingPlayer = gamePlayers.OrderByDescending(gp => gp.Wager).First().PlayerId;
            }

            var nextPlayer = gamePlayers[(gamePlayers.FindIndex(_ => _.PlayerId == trickStartingPlayer) + trick.TrickCard.Count()) % 4];
            return nextPlayer;
        }
    }
}
