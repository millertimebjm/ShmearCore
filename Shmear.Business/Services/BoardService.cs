﻿using Microsoft.EntityFrameworkCore;
using Shmear.Business.Models;
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

        public static async Task SetWager(int gameId, int playerId, int wager)
        {
            var gamePlayer = await GameService.GetGamePlayer(gameId, playerId);
            gamePlayer.Wager = wager;
            await GameService.SaveGamePlayer(gamePlayer);
        }

        public static async Task<RoundResult> EndRound(int gameId)
        {
            var roundResult = await BoardService.DeterminePointsByTeam(gameId);
            return roundResult;
        }

        public static async Task<RoundResult> DeterminePointsByTeam(int gameId)
        {
            var pointList = new List<Point>();
            //var team1Points = 0;
            //var team1Result = 0;
            //var team2Points = 0;
            //var team2Result = 0;
            var team1PlayerSeats = new[]
            {
                1,
                3
            };
            var team2PlayerSeats = new[]
            {
                2,
                4
            };

            using (var db = new CardContext())
            {
                var board = await db.Board.SingleAsync(_ => _.GameId == gameId);
                var gamePlayers = db.GamePlayer.Where(_ => _.GameId == gameId);
                var tricks = db.Trick.Where(_ => _.GameId == gameId);

                var highCard = db.TrickCard.Where(_ => _.Trick.GameId == gameId).Select(_ => _.Card).Where(_ => _.SuitId == board.TrumpSuitId).OrderByDescending(_ => _.Value.Sequence).First();
                var highTrick = tricks.Single(_ => _.TrickCard.Any(card => card.Card.SuitId.Equals(board.TrumpSuitId) && card.Card.Value.Id == highCard.ValueId));
                var highTrickSeatNumber = (await gamePlayers.SingleAsync(_ => _.PlayerId == highTrick.WinningPlayerId)).SeatNumber;
                if (team1PlayerSeats.Contains(highTrickSeatNumber))
                    //team1Points++;
                    pointList.Add(new Point()
                    {
                        Team = 1,
                        PointType = PointTypeEnum.High,
                    });
                if (team2PlayerSeats.Contains(highTrickSeatNumber))
                    //team2Points++;
                    pointList.Add(new Point()
                    {
                        Team = 2,
                        PointType = PointTypeEnum.High,
                    });

                var jackTrick = tricks.SingleOrDefault(_ => _.TrickCard.Any(card => card.Card.SuitId.Equals(board.TrumpSuitId) && card.Card.Value.Name.Equals(CardService.ValueEnum.Jack.ToString())));
                if (jackTrick != null)
                {
                    var point = GetJackPoint(gamePlayers, jackTrick, team1PlayerSeats, team2PlayerSeats);
                    pointList.Add(point);
                }

                var jokerTrick = tricks.SingleOrDefault(_ => _.TrickCard.Any(card => card.Card.Value.Name.Equals(CardService.ValueEnum.Joker.ToString())));
                if (jokerTrick != null)
                {
                    var jokerTrickSeatNumber = gamePlayers.Single(_ => _.PlayerId == jokerTrick.WinningPlayerId).SeatNumber;
                    if (team1PlayerSeats.Contains(jokerTrickSeatNumber))
                        //team1Points++;
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

                var lowCard = db.TrickCard.Where(_ => _.Trick.GameId == gameId).Select(_ => _.Card).Where(_ => _.SuitId == board.TrumpSuitId).OrderBy(_ => _.Value.Sequence).First();
                var lowPlayerId = db.TrickCard.Single(_ => _.Trick.GameId == gameId && _.CardId == lowCard.Id).Player.Id;
                var lowPlayerSeatNumber = gamePlayers.Single(_ => _.PlayerId == lowPlayerId).SeatNumber;
                if (team1PlayerSeats.Contains(lowPlayerSeatNumber))
                    //team1Points++;
                    pointList.Add(new Point()
                    {
                        Team = 1,
                        PointType = PointTypeEnum.Low,
                    });
                if (team2PlayerSeats.Contains(lowPlayerSeatNumber))
                    //team2Points++;
                    pointList.Add(new Point()
                    {
                        Team = 2,
                        PointType = PointTypeEnum.Low,
                    });

                var team1RoundCardScore = 0;
                var team2RoundCardScore = 0;
                foreach (var gamePlayer in gamePlayers)
                {
                    var points = 0;
                    var winningTricks = db.TrickCard.Where(_ => _.Trick.GameId == gameId && _.Trick.WinningPlayerId == gamePlayer.PlayerId).Select(_ => _.Card.Value.Points).ToList();
                    if (winningTricks.Any())
                        points = winningTricks.Sum();
                    if (team1PlayerSeats.Contains(gamePlayer.SeatNumber))
                        team1RoundCardScore += points;
                    if (team2PlayerSeats.Contains(gamePlayer.SeatNumber))
                        team2RoundCardScore += points;
                }

                var wagerSeat = gamePlayers.OrderByDescending(_ => _.Wager).First().SeatNumber;
                if (team1RoundCardScore > team2RoundCardScore)
                {
                    //team1Points++;
                    pointList.Add(new Point()
                    {
                        Team = 1,
                        PointType = PointTypeEnum.Game,
                        OtherData = $" [{team1RoundCardScore} - {team2RoundCardScore}]",
                    });
                }
                else if (team1RoundCardScore < team2RoundCardScore)
                {
                    //team2Points++;
                    pointList.Add(new Point()
                    {
                        Team = 2,
                        PointType = PointTypeEnum.Game,
                        OtherData = $" [{team2RoundCardScore} - {team1RoundCardScore}]",
                    });
                }

                var roundResult = new RoundResult();

                //var bust = 0;
                //team1Result = team1Points;
                roundResult.Team1Points = pointList.Where(_ => _.Team == 1);
                roundResult.Team1RoundChange = roundResult.Team1Points.Count();
                if ((board.Team1Wager ?? 0) > 0 && board.Team1Wager > roundResult.Team1RoundChange)
                {
                    //team1Result = -(int) board.Team1Wager;
                    roundResult.Team1RoundChange = -(int)board.Team1Wager;
                    //bust = 1;
                    roundResult.Bust = true;
                }

                //team2Result = team2Points;
                roundResult.Team2Points = pointList.Where(_ => _.Team == 2);
                roundResult.Team2RoundChange = roundResult.Team2Points.Count();
                if ((board.Team2Wager ?? 0) > 0 && board.Team2Wager > roundResult.Team2RoundChange)
                {
                    //team2Result = -(int) board.Team2Wager;
                    roundResult.Team2RoundChange = -(int)board.Team2Wager;
                    //bust = 1;
                    roundResult.Bust = true;
                }

                roundResult.TeamWager = board.Team1Wager > 0 ? 1 : board.Team2Wager > 0 ? 2 : 0;
                roundResult.TeamWagerValue = Math.Max(board.Team1Wager ?? 0, board.Team2Wager ?? 0);

                return roundResult;
            }
        }

        private static Point GetJackPoint(IEnumerable<GamePlayer> gamePlayers, Trick jackTrick, int[] team1PlayerSeats, int[] team2PlayerSeats)
        {
            var jackTrickSeatNumber = gamePlayers.Single(_ => _.PlayerId == jackTrick.WinningPlayerId).SeatNumber;
            if (team1PlayerSeats.Contains(jackTrickSeatNumber))
                //team1Points++;
                return new Point()
                {
                    Team = 1,
                    PointType = PointTypeEnum.Jack,
                };
            if (team2PlayerSeats.Contains(jackTrickSeatNumber))
                //team2Points++;
                return new Point()
                {
                    Team = 2,
                    PointType = PointTypeEnum.Jack,
                };
            return null;
        }

        public static int DetermineWinningPlayerId(int gameId, IEnumerable<TrickCard> trickCards)
        {
            using (var db = new CardContext())
            {
                var trumpSuitId = (int)db.Board.Single(_ => _.GameId == gameId).TrumpSuitId;
                trickCards = trickCards.OrderBy(_ => _.Sequence);
                var suitLed = trickCards.First();
                var suitLedCard = CardService.GetCard(trickCards.First().CardId);
                var highestCard = false;
                var jokerId = db.Card.Single(_ => _.ValueId == CardService.GetCard(CardService.SuitEnum.None, CardService.ValueEnum.Joker).ValueId).Id;

                var cardTemp = CardService.GetCard(trickCards.First().CardId);
                do
                {
                    int index = 0;
                    foreach (var trickCard in trickCards)
                    {
                        var card = CardService.GetCard(trickCard.CardId);
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
                
                return trickCards.Single(_ => _.CardId == cardTemp.Id).PlayerId;
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
