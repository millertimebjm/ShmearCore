using Microsoft.EntityFrameworkCore;
using Shmear.Business.Models;
using Shmear.EntityFramework.EntityFrameworkCore;
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
        public static int[] GetTeam1PlayerSeats()
        {
            return new[]
            {
                0,
                2
            };
        }

        public static int[] GetTeam2PlayerSeats()
        {
            return new[]
            {
                1,
                3
            };
        }

        public async static Task StartRound(DbContextOptions<CardContext> options, int gameId)
        {
            await CreateBoardIfNotExists(options, gameId);

            await ResetWagers(options, gameId);
            await ResetTrumpCard(options, gameId);

            using (var db = CardContextFactory.Create(options))
            {
                var board = await BoardService.GetBoardByGameId(options, gameId);

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
                board = await SaveBoard(options, board);
            }
        }

        private async static Task ResetWagers(DbContextOptions<CardContext> options, int gameId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var gamePlayers = await db.GamePlayer.Where(_ => _.GameId == gameId).OrderBy(_ => _.SeatNumber).ToListAsync();
                foreach (var gamePlayer in gamePlayers)
                {
                    gamePlayer.Wager = null;
                }
                await db.SaveChangesAsync();
            }
        }

        private static async Task ResetTrumpCard(DbContextOptions<CardContext> options, int gameId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var board = await BoardService.GetBoardByGameId(options, gameId);
                board.TrumpSuitId = null;
                await BoardService.SaveBoard(options, board);
            }
        }

        public static async Task<Board> GetBoardByGameId(DbContextOptions<CardContext> options, int gameId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.Board.SingleAsync(_ => _.GameId == gameId);
            }
        }

        private async static Task CreateBoardIfNotExists(DbContextOptions<CardContext> options, int gameId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var board = await db.Board.SingleOrDefaultAsync(_ => _.GameId == gameId);
                if (board == null)
                {
                    board = await SaveBoard(options, new Board()
                    {
                        GameId = gameId,
                    });
                }
            }
        }

        public async static Task<Board> SaveBoard(DbContextOptions<CardContext> options, Board board)
        {
            using (var db = CardContextFactory.Create(options))
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
                return await GetBoard(options, result.Id);
            }
        }

        public async static Task<Board> GetBoard(DbContextOptions<CardContext> options, int boardId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.Board.SingleAsync(_ => _.Id == boardId);
            }
        }

        public static async Task<bool> DealCards(DbContextOptions<CardContext> options, int gameId)
        {
            var cards = (await CardService.GetCards(options)).Select(_ => _.Id).ToList();

            for (int i = 0; i < (100 + (DateTime.Now.Millisecond % 10)); i++)
            {
                cards = Shuffle(cards);
            }

            var players = (await GameService.GetPlayersByGameAsync(options, gameId)).ToArray();
            for (var i = 0; i < 24; i++)
            {
                await HandService.AddCard(options, gameId, players[(i % 4)].Id, cards[i]);
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

        public static async Task<GamePlayer> GetNextWagerPlayer(DbContextOptions<CardContext> options, int gameId)
        {
            var board = await BoardService.GetBoardByGameId(options, gameId);
            var dealerPlayerId = board.DealerPlayerId;
            var gamePlayers = (await GameService.GetGamePlayers(options, gameId)).OrderBy(_ => _.SeatNumber).ToList();
            var wagerPlayers = gamePlayers.Count(_ => _.Wager != null);
            var nextGamePlayerIndex = ((gamePlayers.FindIndex(_ => _.PlayerId == dealerPlayerId) + wagerPlayers + 1) % 4);
            var nextPlayer = gamePlayers[nextGamePlayerIndex];
            return nextPlayer;
        }

        public static async Task<GamePlayer> GetNextCardPlayer(DbContextOptions<CardContext> options, int gameId, int trickId)
        {
            var board = await BoardService.GetBoardByGameId(options, gameId);
            var firstPlayerId = board.DealerPlayerId;
            var gamePlayers = (await GameService.GetGamePlayers(options, gameId)).OrderBy(_ => _.SeatNumber).ToList();
            var completedTricks = (await TrickService.GetTricks(options, gameId)).Where(_ => _.CompletedDate != null);
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

            var trickCards = await TrickService.GetTrickCards(options, trickId);
            var nextPlayer = gamePlayers[(gamePlayers.FindIndex(_ => _.PlayerId == trickStartingPlayer) + trickCards.Count()) % 4];
            return nextPlayer;
        }

        public static async Task SetWager(DbContextOptions<CardContext> options, int gameId, int playerId, int wager)
        {
            var gamePlayer = await GameService.GetGamePlayer(options, gameId, playerId);
            gamePlayer.Wager = wager;
            await GameService.SaveGamePlayer(options, gamePlayer);

            await CheckBoardWagers(options, gameId);
        }

        private static async Task CheckBoardWagers(DbContextOptions<CardContext> options, int gameId)
        {
            var board = await BoardService.GetBoardByGameId(options, gameId);

            var gamePlayers = (await GameService.GetGamePlayers(options, gameId)).OrderBy(_ => _.SeatNumber).ToArray();
            // if all players have wagered or any player wagered 5
            if (gamePlayers.All(_ => _.Wager != null) || gamePlayers.Any(_ => _.Wager == 5))
            {
                var maxWagerPlayer = gamePlayers.Single(_ => _.Wager == (int)gamePlayers.Max(gp => gp.Wager));
                board.Team1Wager = 0;
                board.Team2Wager = 0;
                if (maxWagerPlayer.SeatNumber == 1 || maxWagerPlayer.SeatNumber == 3)
                    board.Team1Wager = maxWagerPlayer.Wager;
                else
                    board.Team2Wager = maxWagerPlayer.Wager;

                await BoardService.SaveBoard(options, board);
            }
        }

        public static async Task<RoundResult> EndRound(DbContextOptions<CardContext> options, int gameId)
        {
            var roundResult = await BoardService.DeterminePointsByTeam(options, gameId);
            return roundResult;
        }

        public static async Task<RoundResult> DeterminePointsByTeam(DbContextOptions<CardContext> options, int gameId)
        {
            var pointList = new List<Point>();
            var team1PlayerSeats = GetTeam1PlayerSeats();
            var team2PlayerSeats = GetTeam2PlayerSeats();

            using (var db = CardContextFactory.Create(options))
            {
                var board = await db.Board.SingleAsync(_ => _.GameId == gameId);
                var gamePlayers = db.GamePlayer.Where(_ => _.GameId == gameId);
                var tricks = db.Trick.Where(_ => _.GameId == gameId);

                var highTrickCard = db.TrickCard.Where(_ => _.Card.SuitId == board.TrumpSuitId).OrderByDescending(_ => _.Card.Value.Sequence).First();
                var highTrickSeatNumber = gamePlayers.Single(_ => _.PlayerId == highTrickCard.PlayerId).SeatNumber;
                if (team1PlayerSeats.Contains(highTrickSeatNumber))
                    pointList.Add(new Point()
                    {
                        Team = 1,
                        PointType = PointTypeEnum.High,
                    });
                if (team2PlayerSeats.Contains(highTrickSeatNumber))
                    pointList.Add(new Point()
                    {
                        Team = 2,
                        PointType = PointTypeEnum.High,
                    });

                var jackTrick = tricks.SingleOrDefault(_ => _.TrickCard.Any(card => card.Card.SuitId.Equals(board.TrumpSuitId) && card.Card.Value.Name.Equals(CardService.ValueEnum.Jack.ToString())));
                if (jackTrick != null)
                {
                    var point = GetJackPoint(options, gamePlayers, jackTrick, team1PlayerSeats, team2PlayerSeats);
                    pointList.Add(point);
                }

                var jokerTrick = tricks.SingleOrDefault(_ => _.TrickCard.Any(card => card.Card.Value.Name.Equals(CardService.ValueEnum.Joker.ToString())));
                if (jokerTrick != null)
                {
                    var jokerTrickSeatNumber = gamePlayers.Single(_ => _.PlayerId == jokerTrick.WinningPlayerId).SeatNumber;
                    if (team1PlayerSeats.Contains(jokerTrickSeatNumber))
                        pointList.Add(new Point()
                        {
                            Team = 1,
                            PointType = PointTypeEnum.Joker,
                        });
                    if (team2PlayerSeats.Contains(jokerTrickSeatNumber))
                        pointList.Add(new Point()
                        {
                            Team = 2,
                            PointType = PointTypeEnum.Joker,
                        });
                }

                var lowCard = tricks.SelectMany(_ => _.TrickCard).Select(_ => _.Card).Where(_ => _.SuitId == board.TrumpSuitId).OrderBy(_ => _.Value.Sequence).First();
                var lowPlayerId = tricks.SelectMany(_ => _.TrickCard).Single(_ => _.CardId == lowCard.Id).PlayerId;
                var lowPlayerSeatNumber = gamePlayers.Single(_ => _.PlayerId == lowPlayerId).SeatNumber;
                if (team1PlayerSeats.Contains(lowPlayerSeatNumber))
                    pointList.Add(new Point()
                    {
                        Team = 1,
                        PointType = PointTypeEnum.Low,
                    });
                if (team2PlayerSeats.Contains(lowPlayerSeatNumber))
                    pointList.Add(new Point()
                    {
                        Team = 2,
                        PointType = PointTypeEnum.Low,
                    });

                var winningTrickCards = db.GamePlayer.Where(_ => _.GameId == gameId).Select(_ => new KeyValuePair<int, int>(_.SeatNumber, db.TrickCard.Where(tc => tc.PlayerId == _.PlayerId).Sum(tc => tc.Card.Value.Points)));
                var team1RoundCardScore = winningTrickCards.Where(_ => team1PlayerSeats.Contains(_.Key)).Sum(_ => _.Value);
                var team2RoundCardScore = winningTrickCards.Where(_ => team2PlayerSeats.Contains(_.Key)).Sum(_ => _.Value);

                var wagerSeat = gamePlayers.OrderByDescending(_ => _.Wager).First().SeatNumber;
                if (team1RoundCardScore > team2RoundCardScore)
                {
                    pointList.Add(new Point()
                    {
                        Team = 1,
                        PointType = PointTypeEnum.Game,
                        OtherData = $" [{team1RoundCardScore} - {team2RoundCardScore}]",
                    });
                }
                else if (team1RoundCardScore < team2RoundCardScore)
                {
                    pointList.Add(new Point()
                    {
                        Team = 2,
                        PointType = PointTypeEnum.Game,
                        OtherData = $" [{team2RoundCardScore} - {team1RoundCardScore}]",
                    });
                }

                var roundResult = new RoundResult();

                roundResult.Team1Points = pointList.Where(_ => _.Team == 1);
                roundResult.Team1RoundChange = roundResult.Team1Points.Count();
                if ((board.Team1Wager ?? 0) > 0 && board.Team1Wager > roundResult.Team1RoundChange)
                {
                    roundResult.Team1RoundChange = -(int)board.Team1Wager;
                    roundResult.Bust = true;
                }

                roundResult.Team2Points = pointList.Where(_ => _.Team == 2);
                roundResult.Team2RoundChange = roundResult.Team2Points.Count();
                if ((board.Team2Wager ?? 0) > 0 && board.Team2Wager > roundResult.Team2RoundChange)
                {
                    roundResult.Team2RoundChange = -(int)board.Team2Wager;
                    roundResult.Bust = true;
                }

                roundResult.TeamWager = board.Team1Wager > 0 ? 1 : board.Team2Wager > 0 ? 2 : 0;
                roundResult.TeamWagerValue = Math.Max(board.Team1Wager ?? 0, board.Team2Wager ?? 0);

                return roundResult;
            }
        }

        private static Point GetJackPoint(DbContextOptions<CardContext> options, IEnumerable<GamePlayer> gamePlayers, Trick jackTrick, int[] team1PlayerSeats, int[] team2PlayerSeats)
        {
            var jackTrickSeatNumber = gamePlayers.Single(_ => _.PlayerId == jackTrick.WinningPlayerId).SeatNumber;
            if (team1PlayerSeats.Contains(jackTrickSeatNumber))
                return new Point()
                {
                    Team = 1,
                    PointType = PointTypeEnum.Jack,
                };
            if (team2PlayerSeats.Contains(jackTrickSeatNumber))
                return new Point()
                {
                    Team = 2,
                    PointType = PointTypeEnum.Jack,
                };
            return null;
        }

        public static Card DetermineWinningCard(DbContextOptions<CardContext> options, int gameId, IEnumerable<TrickCard> trickCards)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var trumpSuitId = (int)db.Board.Single(_ => _.GameId == gameId).TrumpSuitId;
                trickCards = trickCards.OrderBy(_ => _.Sequence);
                var suitLed = trickCards.First();
                var suitLedCard = CardService.GetCard(options, trickCards.First().CardId);
                var highestCard = false;
                var jokerId = db.Card.Single(_ => _.ValueId == CardService.GetCard(options, CardService.SuitEnum.None, CardService.ValueEnum.Joker).ValueId).Id;

                var cardTemp = CardService.GetCard(options, trickCards.First().CardId);
                do
                {
                    int index = 0;
                    foreach (var trickCard in trickCards)
                    {
                        var card = CardService.GetCard(options, trickCard.CardId);
                        if (CompareCards(trumpSuitId, suitLedCard.SuitId, jokerId, cardTemp, card) < 0)
                        {
                            cardTemp = card;
                            break;
                        }
                        index++;
                    }
                    if (index == 4)
                        highestCard = true;
                } while (!highestCard);

                return cardTemp;
            }
        }

        public static int DetermineWinningPlayerId(DbContextOptions<CardContext> options, int gameId, IEnumerable<TrickCard> trickCards)
        {
            var highestCard = DetermineWinningCard(options, gameId, trickCards);
            using (var db = CardContextFactory.Create(options))
            {
                var trickCard = trickCards.Single(_ => _.CardId == highestCard.Id);
                return trickCard.PlayerId;
            }
        }

        public static int CompareCards(int trumpSuitId, int suitLedId, int jokerId, Card card1, Card card2)
        {
            if ((card1.SuitId == trumpSuitId || card1.Id == jokerId) && card2.SuitId != trumpSuitId && card2.Id != jokerId)
                return 1;
            if (card1.SuitId != trumpSuitId && card1.Id != jokerId && (card2.SuitId == trumpSuitId || card2.Id == jokerId))
                return -1;
            if ((card1.SuitId == trumpSuitId || card1.Id == jokerId) && (card2.SuitId == trumpSuitId || card2.Id == jokerId))
                return CompareValues(card1, card2);
            if (card1.SuitId == suitLedId && card2.SuitId != suitLedId)
                return 1;
            if (card1.SuitId != suitLedId && card2.SuitId == suitLedId)
                return -1;
            return CompareValues(card1, card2);
        }

        public static int CompareValues(Card card1, Card card2)
        {
            var card1Value = (int)Enum.Parse(typeof(CardService.ValueEnum), card1.Value.Name);
            var card2Value = (int)Enum.Parse(typeof(CardService.ValueEnum), card2.Value.Name);
            if (card1Value > card2Value)
                return 1;
            else if (card1Value < card2Value)
                return -1;
            else //if (card1Value == card2Value)
                return 0;
        }
    }
}
