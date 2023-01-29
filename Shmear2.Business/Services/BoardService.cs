using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Shmear2.Business.Database;
using Shmear2.Business.Models;
using Shmear2.Business.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Shmear2.Business.Services
{
    public class BoardService : IBoardService
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

        private readonly CardDbContext _cardDb;
        private readonly ICardService _cardService;
        private readonly IGameService _gameService;
        private readonly IHandService _handService;
        private readonly ITrickService _trickService;
        private readonly IBoardService _boardService;
        public BoardService(
            CardDbContext cardDb,
            ICardService cardService,
            IGameService gameService,
            IHandService handService,
            ITrickService trickService,
            IBoardService boardService)
        {
            _cardDb = cardDb;
            _cardService = cardService;
            _gameService = gameService;
            _handService = handService;
            _trickService = trickService;
            _boardService = boardService;
        }

        public async Task StartRound(int gameId)
        {
            await CreateBoardIfNotExists(gameId);

            await ResetWagers(gameId);
            await ResetTrumpCard(gameId);

            var board = await GetBoardByGameId(gameId);

            var gamePlayers = await _cardDb
                .GamePlayer
                .Where(_ => _.GameId == gameId)
                .OrderBy(_ => _.SeatNumber)
                .ToListAsync();

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
            await SaveBoard(board);
        }

        private async Task ResetWagers(int gameId)
        {
            var gamePlayers = await _cardDb
                .GamePlayer
                .Where(_ => _.GameId == gameId)
                .OrderBy(_ => _.SeatNumber)
                .ToListAsync();
            foreach (var gamePlayer in gamePlayers)
            {
                gamePlayer.Wager = null;
            }
            await _cardDb.SaveChangesAsync();
        }

        private async Task ResetTrumpCard(int gameId)
        {
            var board = await GetBoardByGameId(gameId);
            board.TrumpSuitId = null;
            await SaveBoard(board);
        }

        public async Task<Board> GetBoardByGameId(int gameId)
        {
            return await _cardDb
                .Board
                .SingleAsync(_ => _.GameId == gameId);
        }

        private async Task CreateBoardIfNotExists(int gameId)
        {
            var board = await _cardDb
                .Board
                .SingleOrDefaultAsync(_ => _.GameId == gameId);
            if (board == null)
            {
                await SaveBoard(new Board()
                {
                    GameId = gameId,
                    DateTime = DateTime.Now,
                });
            }
        }

        public async Task<Board> SaveBoard(Board board)
        {
            Board result;
            if (board.Id == 0)
            {
                await _cardDb
                    .Board
                    .AddAsync(board);
                result = board;
            }
            else
            {
                var boardTemp = await _cardDb
                    .Board
                    .SingleAsync(_ => _.Id == board.Id);
                boardTemp.Team1Wager = board.Team1Wager;
                boardTemp.Team2Wager = board.Team2Wager;
                boardTemp.TrumpSuitId = board.TrumpSuitId;
                boardTemp.DealerPlayerId = board.DealerPlayerId;
                result = boardTemp;
            }

            await _cardDb.SaveChangesAsync();
            return await GetBoard(result.Id);
        }

        public async Task<Board> GetBoard(int boardId)
        {
            return await _cardDb
                .Board
                .SingleAsync(_ => _.Id == boardId);
        }

        public async Task<bool> DealCards(int gameId)
        {
            var cards = (await _cardService.GetCards()).Select(_ => _.Id).ToList();

            for (int i = 0; i < (100 + (DateTime.Now.Millisecond % 10)); i++)
            {
                cards = Shuffle(cards);
            }

            var players = (await _gameService.GetPlayersByGameAsync(gameId))
                .ToArray();
            for (var i = 0; i < 24; i++)
            {
                await _handService.AddCard(gameId, players[(i % 4)].Id, cards[i]);
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
                do provider.GetBytes(box); while (box[0] >= n * (byte.MaxValue / n));
                var k = (box[0] % n);
                n--;
                var value = cards[k];
                cards[k] = cards[n];
                cards[n] = value;
            }
            return cards;
        }

        public async Task<GamePlayer> GetNextWagerPlayer(int gameId)
        {
            var board = await GetBoardByGameId(gameId);
            var dealerPlayerId = board.DealerPlayerId;
            var gamePlayers = (await _gameService.GetGamePlayers(gameId)).OrderBy(_ => _.SeatNumber).ToList();
            var wagerPlayers = gamePlayers.Count(_ => _.Wager != null);
            var nextGamePlayerIndex = ((gamePlayers.FindIndex(_ => _.PlayerId == dealerPlayerId) + wagerPlayers + 1) % 4);
            var nextPlayer = gamePlayers[nextGamePlayerIndex];
            return nextPlayer;
        }

        public async Task<GamePlayer> GetNextCardPlayer(int gameId, int trickId)
        {
            var gamePlayers = (await _gameService.GetGamePlayers(gameId)).OrderBy(_ => _.SeatNumber).ToList();
            var completedTricks = (await _trickService.GetTricks(gameId)).Where(_ => _.CompletedDate != null);
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

            var trickCards = await _trickService.GetTrickCards(trickId);
            var nextPlayer = gamePlayers[(gamePlayers.FindIndex(_ => _.PlayerId == trickStartingPlayer) + trickCards.Count()) % 4];
            return nextPlayer;
        }

        public async Task SetWager(int gameId, int playerId, int wager)
        {
            var gamePlayer = await _gameService.GetGamePlayer(gameId, playerId);
            gamePlayer.Wager = wager;
            await _gameService.SaveGamePlayer(gamePlayer);

            await CheckBoardWagers(gameId);
        }

        private async Task CheckBoardWagers(int gameId)
        {
            var board = await GetBoardByGameId(gameId);

            var gamePlayers = (await _gameService.GetGamePlayers(gameId))
                .OrderBy(_ => _.SeatNumber)
                .ToArray();
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

                await SaveBoard(board);
            }
        }

        public async Task<RoundResult> EndRound(int gameId)
        {
            var roundResult = await DeterminePointsByTeam(gameId);
            return roundResult;
        }

        // TODO: Desperately needs to have method extraction
        public async Task<RoundResult> DeterminePointsByTeam(int gameId)
        {
            var pointList = new List<Point>();
            var team1PlayerSeats = GetTeam1PlayerSeats();
            var team2PlayerSeats = GetTeam2PlayerSeats();

            var board = await _cardDb.Board.SingleAsync(_ => _.GameId == gameId);
            var gamePlayers = _cardDb.GamePlayer.Where(_ => _.GameId == gameId);
            var tricks = _cardDb.Trick.Where(_ => _.GameId == gameId);

            var highTrickCard = _cardDb.TrickCard.Where(_ => _.Card.SuitId == board.TrumpSuitId).OrderByDescending(_ => _.Card.Value.Sequence).First();
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

            var jackTrick = tricks.SingleOrDefault(_ => _.TrickCard.Any(card => card.Card.SuitId.Equals(board.TrumpSuitId) && card.Card.Value.Name.Equals(ValueEnum.Jack.ToString())));
            if (jackTrick != null)
            {
                var point = GetJackPoint(gamePlayers, jackTrick, team1PlayerSeats, team2PlayerSeats);
                pointList.Add(point);
            }

            var jokerTrick = tricks.SingleOrDefault(_ => _.TrickCard.Any(card => card.Card.Value.Name.Equals(ValueEnum.Joker.ToString())));
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

            var winningTrickCards = _cardDb.GamePlayer.Where(_ => _.GameId == gameId).Select(_ => new KeyValuePair<int, int>(_.SeatNumber, _cardDb.TrickCard.Where(tc => tc.PlayerId == _.PlayerId).Sum(tc => tc.Card.Value.Points)));
            var team1RoundCardScore = winningTrickCards.Where(_ => team1PlayerSeats.Contains(_.Key)).Sum(_ => _.Value);
            var team2RoundCardScore = winningTrickCards.Where(_ => team2PlayerSeats.Contains(_.Key)).Sum(_ => _.Value);

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

            roundResult.TeamWager = 0;
            if (board.Team1Wager > 0)
                roundResult.TeamWager = 1;
            if (board.Team2Wager > 0)
                roundResult.TeamWager = 2;

            roundResult.TeamWagerValue = Math.Max(board.Team1Wager ?? 0, board.Team2Wager ?? 0);

            return roundResult;
        }

        private static Point GetJackPoint(
            IEnumerable<GamePlayer> gamePlayers,
            Trick jackTrick,
            int[] team1PlayerSeats,
            int[] team2PlayerSeats)
        {
            var jackTrickSeatNumber = gamePlayers
                .Single(_ => _.PlayerId == jackTrick.WinningPlayerId)
                .SeatNumber;
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

        public Card DetermineWinningCard(int gameId, IEnumerable<TrickCard> trickCards)
        {
            var trumpSuitId = (int)_cardDb.
                Board
                .Single(_ => _.GameId == gameId)
                .TrumpSuitId;
            trickCards = trickCards.OrderBy(_ => _.Sequence);
            var suitLedCard = _cardService.GetCard(trickCards.First().CardId);
            var highestCard = false;
            var jokerCard = _cardService
                .GetCard(SuitEnum.None, ValueEnum.Joker)
                .ValueId;
            var jokerId = _cardDb
                .Card
                .Single(_ => _.ValueId == jokerCard)
                .Id;

            var cardTemp = _cardService.GetCard(trickCards.First().CardId);
            do
            {
                int index = 0;
                foreach (var trickCard in trickCards)
                {
                    var card = _cardService.GetCard(trickCard.CardId);
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

        public int DetermineWinningPlayerId(int gameId, IEnumerable<TrickCard> trickCards)
        {
            var highestCard = _boardService.DetermineWinningCard(gameId, trickCards);
            var trickCard = trickCards.Single(_ => _.CardId == highestCard.Id);
            return trickCard.PlayerId;
        }

        public static int CompareCards(int trumpSuitId, int suitLedId, int jokerId, Card card1, Card card2)
        {
            if ((card1.SuitId == trumpSuitId || card1.Id == jokerId) && card2.SuitId != trumpSuitId && card2.Id != jokerId)
                return 1;
            if (card1.SuitId != trumpSuitId && card1.Id != jokerId && (card2.SuitId == trumpSuitId || card2.Id == jokerId))
                return -1;
            if ((card1.SuitId == trumpSuitId || card1.Id == jokerId) && (card2.SuitId == trumpSuitId || card2.Id == jokerId))
                return BoardService.CompareValues(card1, card2);
            if (card1.SuitId == suitLedId && card2.SuitId != suitLedId)
                return 1;
            if (card1.SuitId != suitLedId && card2.SuitId == suitLedId)
                return -1;
            return BoardService.CompareValues(card1, card2);
        }

        public static int CompareValues(Card card1, Card card2)
        {
            var card1Value = (int)Enum.Parse(typeof(ValueEnum), card1.Value.Name);
            var card2Value = (int)Enum.Parse(typeof(ValueEnum), card2.Value.Name);
            if (card1Value > card2Value)
                return 1;
            else if (card1Value < card2Value)
                return -1;
            else
                return 0;
        }
    }
}
