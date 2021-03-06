﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shmear.EntityFramework.EntityFrameworkCore.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore;

namespace Shmear.Business.Services
{
    public static class GameService
    {
        public async static Task<Game> GetOpenGame(DbContextOptions<CardContext> options)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var openGames = await db.Game.Where(_ => _.StartedDate == null && _.GamePlayer.Count < 4).ToListAsync();
                if (openGames.Any())
                {
                    return openGames.First();
                }

                return await CreateGame(options);
            }
        }

        public async static Task<Game> CreateGame(DbContextOptions<CardContext> options)
        {
            var game = new Game
            {
                Id = 0,
                CreatedDate = DateTime.Now,
                StartedDate = null
            };

            using (var db = CardContextFactory.Create(options))
            {
                await db.Game.AddAsync(game);
                await db.SaveChangesAsync();
            }
            return await GetGame(options, game.Id);
        }

        public async static Task<Game> GetGame(DbContextOptions<CardContext> options, int id)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.Game.SingleOrDefaultAsync(_ => _.Id == id);
            }
        }

        public async static Task<IEnumerable<GamePlayer>> GetGamePlayers(DbContextOptions<CardContext> options, int gameId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.GamePlayer.Include(p => p.Player).Where(_ => _.GameId == gameId).ToListAsync();
            }
        }

        public async static Task<int> AddPlayer(DbContextOptions<CardContext> options, int gameId, int playerId, int seatNumber)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var game = await db.Game.SingleAsync(_ => _.Id == gameId);
                var player = await db.Player.SingleAsync(_ => _.Id == playerId);
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
                        var gamePlayerToRemove = await db.GamePlayer.SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
                        db.GamePlayer.Remove(gamePlayerToRemove);
                    }

                    await db.GamePlayer.AddAsync(gamePlayer);
                    player.KeepAlive = DateTime.Now;
                    return await db.SaveChangesAsync();
                }

                return 0;
            }
        }

        public async static Task<bool> RemovePlayer(DbContextOptions<CardContext> options, int gameId, int playerId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var gamePlayer = await db.GamePlayer.SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
                db.GamePlayer.Remove(gamePlayer);
                await db.SaveChangesAsync();
                return true;
            }
        }

        public async static Task<GamePlayer> GetGamePlayer(DbContextOptions<CardContext> options, int gameId, int playerId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.GamePlayer.Include(_ => _.Player).SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
            }
        }

        public async static Task<GamePlayer> GetGamePlayer(DbContextOptions<CardContext> options, int gameId, string connectionId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.GamePlayer.SingleAsync(_ => _.GameId == gameId && _.Player.ConnectionId == connectionId);
            }
        }

        public async static Task<GamePlayer> SaveGamePlayer(DbContextOptions<CardContext> options, GamePlayer gamePlayer)
        {
            using (var db = CardContextFactory.Create(options))
            {
                GamePlayer gamePlayerReturn;
                if (gamePlayer.Id == 0)
                {
                    await db.GamePlayer.AddAsync(gamePlayer);
                    gamePlayerReturn = gamePlayer;
                }
                else
                {
                    var gamePlayerTemp = await db.GamePlayer.SingleAsync(_ => _.Id == gamePlayer.Id);
                    gamePlayerTemp.SeatNumber = gamePlayer.SeatNumber;
                    gamePlayerTemp.Wager = gamePlayer.Wager;
                    gamePlayerTemp.Ready = gamePlayer.Ready;
                    gamePlayerReturn = gamePlayerTemp;
                }
                await db.SaveChangesAsync();
                return await GameService.GetGamePlayer(options, gamePlayerReturn.GameId, gamePlayerReturn.PlayerId);
            }
        }

        public async static Task<bool> StartGame(DbContextOptions<CardContext> options, int gameId)
        {
            using (var db = CardContextFactory.Create(options))
            {

                var game = await db.Game.SingleAsync(_ => _.Id == gameId);
                if (game.StartedDate == null)
                {
                    game.StartedDate = DateTime.Now;
                    await db.SaveChangesAsync();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static async Task<IEnumerable<Player>> GetPlayersByGameAsync(DbContextOptions<CardContext> options, int gameId)
        {
            return (await GetGamePlayers(options, gameId)).Select(_ => _.Player);
        }

        public static async Task<bool> ValidCardPlay(DbContextOptions<CardContext> options, int gameId, int boardId, int playerId, int cardId)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var gamePlayer = await db.GamePlayer.Include(p => p.Player).SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
                var player = gamePlayer.Player;

                var cards = await HandService.GetHand(options, gameId, player.Id);
                var tricks = await TrickService.GetTricks(options, gameId);
                var trick = tricks.SingleOrDefault(_ => _.CompletedDate == null);
                if (trick == null || trick.Id == 0)
                {
                    trick = await TrickService.CreateTrick(options, gameId);
                }

                var board = await BoardService.GetBoard(options, boardId);
                var trumpSuitId = board.TrumpSuitId ?? 0;
                var card = await CardService.GetCardAsync(options, cardId);
                
                if (trumpSuitId == 0)
                {
                    if (card.Value.Name == CardService.ValueEnum.Joker.ToString())
                        return false;

                    return true;
                }
                else
                {
                    var trickCards = await TrickService.GetTrickCards(options, trick.Id);
                    if (trickCards.Any())
                    {
                        var cardLed = (await CardService.GetCardAsync(options, trickCards.First().CardId));
                        var suitLedId = cardLed.SuitId;
                        if (card.SuitId == trumpSuitId)
                            return true;
                        if (card.Value.Name == CardService.ValueEnum.Joker.ToString())
                            return true;
                        if (suitLedId == card.SuitId)
                            return true;
                        if (cards.All(_ => CardService.GetCard(options, _.Card.Id).SuitId != suitLedId)
                            && cards.All(_ => CardService.GetCard(options, _.Card.Id).Value.Name != CardService.ValueEnum.Joker.ToString()))
                            return true;

                        return false;
                    }
                    else
                        return true;
                }
            }
        }

        public static async Task<Game> SaveRoundChange(DbContextOptions<CardContext> options, int gameId, int team1Points, int team2Points)
        {
            using (var db = CardContextFactory.Create(options))
            {
                var game = await db.Game.SingleAsync(_ => _.Id == gameId);
                game.Team1Points = team1Points;
                game.Team2Points = team2Points;
                await db.SaveChangesAsync();
                return game;
            }
        }
    }
}
