using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Shmear2.Business.Database;
using Shmear2.Business.Models;
using Shmear2.Business.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Shmear2.Business.Services
{
    public class ShmearService : IShmearService
    {
        private readonly CardDbContext _cardDb;
        public ShmearService(
            CardDbContext cardDb)
        {
            _cardDb = cardDb;

            if (!HasAnyCard())
            {
                SeedValues();
                SeedSuits();
                SeedCards();
            }
        }

        public static int[] GetTeam1PlayerSeats()
        {
            return new[]
            {
                1,
                3
            };
        }

        public static int[] GetTeam2PlayerSeats()
        {
            return new[]
            {
                2,
                4
            };
        }

        public async Task<Game> GetOpenGame()
        {
            var openGames = await _cardDb
                .Game
                .Where(_ => _.StartedDate == null && _.GamePlayer.Count < 4)
                .ToListAsync();
            if (openGames.Any())
            {
                return openGames.First();
            }

            return await CreateGame();
        }

        public async Task<Game> CreateGame()
        {
            var game = new Game
            {
                Id = 0,
                CreatedDate = DateTime.Now,
                StartedDate = null
            };

            await _cardDb.Game.AddAsync(game);
            await _cardDb.SaveChangesAsync();

            return await GetGame(game.Id);
        }

        public async Task<Game> GetGame(int id)
        {
            return await _cardDb.Game.SingleOrDefaultAsync(_ => _.Id == id);
        }

        public async Task<IEnumerable<GamePlayer>> GetGamePlayers(int gameId)
        {
            return await _cardDb
                .GamePlayer
                .Include(p => p.Player)
                .Where(_ => _.GameId == gameId)
                .ToListAsync();
        }

        public async Task<IEnumerable<GamePlayer>> GetHumanGamePlayers(int gameId)
        {
            return await _cardDb
                .GamePlayer
                .Include(p => p.Player)
                .Where(_ => _.GameId == gameId
                    && _.Player.ConnectionId != null
                    && _.Player.ConnectionId != "")
                .ToListAsync();
        }

        public async Task<int> AddPlayer(int gameId, int playerId, int seatNumber)
        {
            var game = await _cardDb.Game.SingleAsync(_ => _.Id == gameId);
            var player = await _cardDb.Player.SingleAsync(_ => _.Id == playerId);
            var gamePlayers = game.GamePlayer.OrderBy(_ => _.SeatNumber);

            if (!gamePlayers.Any(_ => _.SeatNumber == seatNumber))
            {
                var gamePlayer = new GamePlayer()
                {
                    GameId = gameId,
                    PlayerId = playerId,
                    SeatNumber = seatNumber,
                    Ready = false,
                };

                if (game.GamePlayer.Any(_ => _.PlayerId == playerId))
                {
                    var gamePlayerToRemove = await _cardDb.GamePlayer.SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
                    _cardDb.GamePlayer.Remove(gamePlayerToRemove);
                }

                await _cardDb.GamePlayer.AddAsync(gamePlayer);
                player.KeepAlive = DateTime.Now;
                return await _cardDb.SaveChangesAsync();
            }

            return 0;
        }

        public async Task<bool> RemovePlayer(int gameId, int playerId)
        {
            var gamePlayer = await _cardDb
                .GamePlayer
                .SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
            _cardDb.GamePlayer.Remove(gamePlayer);
            await _cardDb.SaveChangesAsync();
            return true;
        }

        public async Task<GamePlayer> GetGamePlayer(int gameId, int playerId)
        {
            return await _cardDb
                .GamePlayer
                .Include(_ => _.Player)
                .SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
        }

        public async Task<GamePlayer> GetGamePlayer(int gamePlayerId)
        {
            return await _cardDb
                .GamePlayer
                .SingleAsync(_ => _.Id == gamePlayerId);
        }

        public async Task<GamePlayer> GetGamePlayer(int gameId, string connectionId)
        {
            return await _cardDb
                .GamePlayer
                .SingleAsync(_ => _.GameId == gameId && _.Player.ConnectionId == connectionId);
        }

        public async Task<GamePlayer> SaveGamePlayer(GamePlayer gamePlayer)
        {
            GamePlayer gamePlayerReturn;
            if (gamePlayer.Id == 0)
            {
                await _cardDb.GamePlayer.AddAsync(gamePlayer);
                gamePlayerReturn = gamePlayer;
            }
            else
            {
                var gamePlayerTemp = await _cardDb.GamePlayer.SingleAsync(_ => _.Id == gamePlayer.Id);
                gamePlayerTemp.SeatNumber = gamePlayer.SeatNumber;
                gamePlayerTemp.Wager = gamePlayer.Wager;
                gamePlayerTemp.Ready = gamePlayer.Ready;
                gamePlayerReturn = gamePlayerTemp;
            }
            await _cardDb.SaveChangesAsync();
            return await GetGamePlayer(gamePlayerReturn.GameId, gamePlayerReturn.PlayerId);
        }

        public async Task<bool> StartGame(int gameId)
        {
            var game = await _cardDb.Game.SingleAsync(_ => _.Id == gameId);
            if (game.StartedDate == null)
            {
                game.StartedDate = DateTime.Now;
                await _cardDb.SaveChangesAsync();
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<IEnumerable<Player>> GetPlayersByGameAsync(int gameId)
        {
            return (await GetGamePlayers(gameId)).Select(_ => _.Player);
        }

        public async Task<bool> ValidCardPlay(int gameId, int boardId, int playerId, int cardId)
        {
            var gamePlayer = await _cardDb.GamePlayer
                .Include(p => p.Player)
                .SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
            var player = gamePlayer.Player;

            var cards = await GetHand(gameId, player.Id);
            var tricks = await GetTricks(gameId);
            var trick = tricks.SingleOrDefault(_ => _.CompletedDate == null);
            if (trick == null || trick.Id == 0)
            {
                trick = await CreateTrick(gameId);
            }

            var board = await GetBoard(boardId);
            var trumpSuitId = board.TrumpSuitId ?? 0;
            var card = await GetCardAsync(cardId);

            if (trumpSuitId == 0)
            {
                if (card.Value.Name == ValueEnum.Joker.ToString())
                    return false;

                return true;
            }
            else
            {
                var trickCards = await GetTrickCards(trick.Id);
                if (trickCards.Any())
                {
                    var cardLed = (await GetCardAsync(trickCards.OrderBy(_ => _.Sequence)
                        .First().CardId));
                    var suitLedId = cardLed.SuitId;
                    if (card.SuitId == trumpSuitId)
                        return true;
                    if (card.Value.Name == ValueEnum.Joker.ToString())
                        return true;
                    if (suitLedId == card.SuitId)
                        return true;
                    if (cards.All(_ => GetCard(_.Card.Id).SuitId != suitLedId)
                        && cards.All(_ => GetCard(_.Card.Id).Value.Name != ValueEnum.Joker.ToString()))
                        return true;

                    return false;
                }
                else
                    return true;
            }
        }

        public async Task<Game> SaveRoundChange(int gameId, int team1Points, int team2Points)
        {
            var game = await _cardDb.Game.SingleAsync(_ => _.Id == gameId);
            game.Team1Points = team1Points;
            game.Team2Points = team2Points;
            await _cardDb.SaveChangesAsync();
            return game;
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
            var cards = (await GetCards()).Select(_ => _.Id).ToList();

            for (int i = 0; i < (100 + (DateTime.Now.Millisecond % 10)); i++)
            {
                cards = Shuffle(cards);
            }

            var players = (await GetPlayersByGameAsync(gameId))
                .ToArray();
            for (var i = 0; i < 24; i++)
            {
                await AddCard(gameId, players[(i % 4)].Id, cards[i]);
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

        public async Task<GamePlayer> GetNextWagerGamePlayer(int gameId)
        {
            var board = await GetBoardByGameId(gameId);
            var dealerPlayerId = board.DealerPlayerId;
            var gamePlayers = (await GetGamePlayers(gameId)).OrderBy(_ => _.SeatNumber).ToList();
            var wagerPlayers = gamePlayers.Count(_ => _.Wager != null);
            if (wagerPlayers < 4)
            {
                var nextGamePlayerIndex = ((gamePlayers.FindIndex(_ => _.PlayerId == dealerPlayerId) + wagerPlayers + 1) % 4);
                var nextPlayer = gamePlayers[nextGamePlayerIndex];
                return nextPlayer;
            }
            return null;
        }

        public async Task<int> GetHighestWager(int gameId)
        {
            var board = await GetBoardByGameId(gameId);
            var gamePlayers = (await GetGamePlayers(gameId)).OrderBy(_ => _.SeatNumber).ToList();
            var maxWager = gamePlayers.Max(_ => _.Wager) ?? 1;
            return maxWager;
        }

        public async Task<GamePlayer> GetNextCardGamePlayer(int gameId)
        {
            var trick = _cardDb
                .Trick
                .Where(_ => _.GameId == gameId && _.CompletedDate == null)
                .Single();
            return await GetNextCardGamePlayer(gameId, trick.Id);
        }

        public async Task<GamePlayer> GetNextCardGamePlayer(int gameId, int trickId)
        {
            var gamePlayers = (await GetGamePlayers(gameId))
                .OrderBy(_ => _.SeatNumber)
                .ToList();
            var completedTricks = (await GetTricks(gameId))
                .Where(_ => _.CompletedDate != null);
            int trickStartingPlayer;
            if (completedTricks.Any())
            {
                var latestCompletedTrick = completedTricks
                    .OrderByDescending(_ => _.CompletedDate)
                    .First();
                trickStartingPlayer = (int)latestCompletedTrick.WinningPlayerId;
            }
            else
            {
                trickStartingPlayer = gamePlayers.OrderByDescending(gp => gp.Wager).First().PlayerId;
            }

            var trickCards = await GetTrickCards(trickId);
            var nextPlayer = gamePlayers[(gamePlayers.FindIndex(_ => _.PlayerId == trickStartingPlayer) + trickCards.Count()) % 4];
            return nextPlayer;
        }

        public async Task<bool> SetWager(int gameId, int playerId, int wager)
        {
            var nextWagerGamePlayer = await GetNextWagerGamePlayer(gameId);
            var gamePlayer = await GetGamePlayer(gameId, playerId);
            if (gamePlayer.Id == nextWagerGamePlayer.Id)
            {
                gamePlayer.Wager = wager;
                await SaveGamePlayer(gamePlayer);

                await CheckBoardWagers(gameId);
                return true;
            }
            return false;
        }

        private async Task CheckBoardWagers(int gameId)
        {
            var board = await GetBoardByGameId(gameId);

            var gamePlayers = (await GetGamePlayers(gameId))
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
                await CreateTrick(gameId);
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

            var highTrickCard = _cardDb
                .TrickCard
                .Where(_ => _.Card.SuitId == board.TrumpSuitId)
                .OrderByDescending(_ => _.Card.Value.Sequence)
                .First();
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

            var jackTrick = tricks
                .SingleOrDefault(_ => _.TrickCard.Any(card => card.Card.SuitId.Equals(board.TrumpSuitId) && card.Card.Value.Name.Equals(ValueEnum.Jack.ToString())));
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

            roundResult.Team1PointTypes = pointList.Where(_ => _.Team == 1);
            roundResult.Team1RoundChange = roundResult.Team1PointTypes.Count();
            if ((board.Team1Wager ?? 0) > 0 && board.Team1Wager > roundResult.Team1RoundChange)
            {
                roundResult.Team1RoundChange = -(int)board.Team1Wager;
                roundResult.Bust = true;
            }

            roundResult.Team2PointTypes = pointList.Where(_ => _.Team == 2);
            roundResult.Team2RoundChange = roundResult.Team2PointTypes.Count();
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

        public async Task<MatchResult> EndMatch(int gameId, RoundResult roundResult)
        {
            var game = _cardDb.Game.Single(_ => _.Id == gameId);
            var matchResult = new MatchResult();
            if (roundResult.Team1Points >= 11 && roundResult.Team2Points >= 11)
            {
                if (roundResult.TeamWager == 1) matchResult.TeamMatchWinner = 1;
                else matchResult.TeamMatchWinner = 2;
            }
            else if (roundResult.Team1Points >= 11) matchResult.TeamMatchWinner = 1;
            else if (roundResult.Team2Points >= 11) matchResult.TeamMatchWinner = 2;

            if (matchResult.TeamMatchWinner == 1) game.Team1Matches++;
            if (matchResult.TeamMatchWinner == 2) game.Team2Matches++;
            matchResult.Team1Matches = game.Team1Matches;
            matchResult.Team2Matches = game.Team2Matches;

            return matchResult;
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
            var suitLedCard = GetCard(trickCards.First().CardId);
            var highestCard = false;
            var jokerCard = GetCard(SuitEnum.None, ValueEnum.Joker).ValueId;
            var jokerId = _cardDb
                .Card
                .Single(_ => _.ValueId == jokerCard)
                .Id;

            var cardTemp = GetCard(trickCards.First().CardId);
            do
            {
                int index = 0;
                foreach (var trickCard in trickCards)
                {
                    var card = GetCard(trickCard.CardId);
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
            var highestCard = DetermineWinningCard(gameId, trickCards);
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
                return CompareValues(card1, card2);
            if (card1.SuitId == suitLedId && card2.SuitId != suitLedId)
                return 1;
            if (card1.SuitId != suitLedId && card2.SuitId == suitLedId)
                return -1;
            return CompareValues(card1, card2);
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

        public async Task<IEnumerable<Card>> GetCards()
        {
            return await _cardDb
                .Card
                .ToListAsync();
        }

        public async Task<Card> GetCardAsync(int id)
        {
            return await _cardDb
                .Card
                .Include(s => s.Suit)
                .Include(_ => _.Value)
                .SingleAsync(_ => _.Id == id);
        }

        public Card GetCard(int id)
        {
            return _cardDb
                .Card
                .Include(s => s.Suit)
                .Include(_ => _.Value)
                .Single(_ => _.Id == id);
        }

        public Card GetCard(SuitEnum suit, ValueEnum value)
        {
            return _cardDb.Card.Single(_ => _.Suit.Name == suit.ToString() && _.Value.Name == value.ToString());
        }

        public bool SeedSuits()
        {
            var suits = new List<Suit>()
            {
                new Suit()
                {
                    Name = "Clubs",
                    Char = "♣"
                },
                new Suit()
                {
                    Name = "Spades",
                    Char = "♠"
                },
                new Suit()
                {
                    Name = "Diamonds",
                    Char = "♦"
                },
                new Suit()
                {
                    Name = "Hearts",
                    Char = "♥"
                },
                new Suit()
                {
                    Name = "None",
                    Char = " "
                }
            };

            foreach (var suit in suits)
            {
                if (!_cardDb.Suit.Any(_ => _.Name.Equals(suit.Name)))
                {
                    _cardDb.Suit.Add(suit);
                    _cardDb.SaveChanges();
                }
            }
            return true;
        }

        public bool HasAnyCard()
        {
            return _cardDb.Card.Any();
        }
        public bool SeedValues()
        {
            var values = new List<Value>()
            {
                new Value()
                {
                    Name = "Seven",
                    Char = "7",
                    Points = 0,
                    Sequence = 10
                },
                new Value()
                {
                    Name = "Eight",
                    Char = "8",
                    Points = 0,
                    Sequence = 20
                },
                new Value()
                {
                    Name = "Nine",
                    Char = "9",
                    Points = 0,
                    Sequence = 30
                },
                new Value()
                {
                    Name = "Ten",
                    Char = "T",
                    Points = 10,
                    Sequence = 40
                },
                new Value()
                {
                    Name = "Joker",
                    Char = "J",
                    Points = 1,
                    Sequence = 50
                },
                new Value()
                {
                    Name = "Jack",
                    Char = "J",
                    Points = 1,
                    Sequence = 60
                },
                new Value()
                {
                    Name = "Queen",
                    Char = "Q",
                    Points = 2,
                    Sequence = 70
                },
                new Value()
                {
                    Name = "King",
                    Char = "K",
                    Points = 3,
                    Sequence = 80
                },
                new Value()
                {
                    Name = "Ace",
                    Char = "A",
                    Points = 4,
                    Sequence = 90
                },
            };

            foreach (var value in values)
            {
                if (!_cardDb.Value.Any(_ => _.Name.Equals(value.Name)))
                {
                    _cardDb.Value.Add(value);
                    _cardDb.SaveChanges();
                }
            }
            return true;
        }

        public async Task<Card> GetCard(int suitId, ValueEnum valueEnum)
        {
            return await _cardDb
                .Card
                .Include(s => s.Suit)
                .Include(_ => _.Value)
                .SingleAsync(_ => _.SuitId == suitId && _.Value.Name == valueEnum.ToString());
        }

        public bool SeedCards()
        {
            foreach (SuitEnum suit in Enum.GetValues(typeof(SuitEnum)))
            {
                if (suit != SuitEnum.None)
                {
                    foreach (ValueEnum value in Enum.GetValues(typeof(ValueEnum)))
                    {
                        if (value != ValueEnum.Joker)
                        {
                            var suitId = _cardDb.Suit.Single(_ => _.Name.Equals(suit.ToString())).Id;
                            var valueId = _cardDb.Value.Single(_ => _.Name.Equals(value.ToString())).Id;
                            if (!_cardDb.Card.Any(_ => _.SuitId == suitId && _.ValueId == valueId))
                            {
                                var card = new Card()
                                {
                                    SuitId = suitId,
                                    ValueId = valueId
                                };
                                _cardDb.Card.Add(card);
                                _cardDb.SaveChanges();
                            }
                        }
                    }
                }
            }
            var noneSuitId = _cardDb.Suit.Single(_ => _.Name.Equals(SuitEnum.None.ToString())).Id;
            var jokerValueId = _cardDb.Value.Single(_ => _.Name.Equals(ValueEnum.Joker.ToString())).Id;
            if (!_cardDb.Card.Any(_ => _.SuitId == noneSuitId && _.ValueId == jokerValueId))
            {
                var joker = new Card()
                {
                    SuitId = noneSuitId,
                    ValueId = jokerValueId
                };

                _cardDb.Card.Add(joker);
                _cardDb.SaveChanges();
            }
            return true;
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

        public async Task<IEnumerable<Trick>> GetTricks(int gameId)
        {
            return await _cardDb.Trick.Where(_ => _.GameId == gameId).ToListAsync();
        }

        public async Task<Trick> GetTrick(int trickId)
        {
            return await _cardDb.Trick.SingleAsync(_ => _.Id == trickId);
        }

        public async Task<Trick?> GetIncompleteTrick(int gameId)
        {
            return await _cardDb.Trick.SingleOrDefaultAsync(_ => _.CompletedDate == null);
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
            trick.WinningPlayerId = DetermineWinningPlayerId(trick.GameId, trickCards);
            await _cardDb.SaveChangesAsync();

            return await GetTrick(trick.Id);
        }

        // public async Task<bool> PlayCard(int gameId, int playerId, int cardId)
        // {
        //     if (!ValidCardPlay(gameId, playerId, cardId))
        //     {
        //         return false;
        //     }
        //     var incompleteTrick = await GetIncompleteTrick(gameId);
        //     var highestSequence = incompleteTrick?.Sequence ?? 0;
        //     var trickCard = new TrickCard()
        //     {
        //         CardId = cardId,
        //         PlayerId = playerId,
        //         Sequence = highestSequence + 1,
        //         TrickId = incompleteTrick.Id
        //     };
        //     await _cardDb.TrickCard.AddAsync(trickCard);
        //     await _cardDb.SaveChangesAsync();

        //     HandCard handCard = _cardDb
        //         .HandCard
        //         .Single(_ => _.GameId == gameId && _.PlayerId == playerId && _.CardId == cardId);
        //     _cardDb.HandCard.Remove(handCard);

        //     var gameId = trick.GameId;
        //     var board = await _cardDb.Board.SingleAsync(_ => _.GameId == gameId);
        //     if (board.TrumpSuitId == null || board.TrumpSuitId == 0)
        //         board.TrumpSuitId = _cardDb.Card.Single(_ => _.Id == cardId).SuitId;

        //     _cardDb.SaveChanges();
        // }

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
