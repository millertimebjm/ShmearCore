﻿using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Shmear2.Business.Services;
using Shmear2.Business.Database.Models;
using Shmear2.Business.Models;
using Shmear2.Business.Database;

namespace Shmear2.Mvc.Hubs
{
    public class ShmearHub : Hub
    {
        private readonly CardDbContext _cardDb;
        private readonly IPlayerService _playerService;
        private readonly IShmearService _shmearService;

        public ShmearHub(
            CardDbContext cardDb,
            IPlayerService playerService,
            IShmearService shmearService
        )
            : base()
        {
            _cardDb = cardDb;
            _playerService = playerService;
            _shmearService = shmearService;
        }

        public async override Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            string userName = Context.User.Identity.Name ?? "";
            string connectionId = Context.ConnectionId;

            var player = await _playerService.GetPlayer(connectionId);

            if (player == null || player.Id == 0)
            {
                player = new Player()
                {
                    ConnectionId = connectionId,
                    Name = userName
                };
            }

            await _playerService.SavePlayer(player);
        }

        public async Task SetPlayerName(string name)
        {
            var player = await _playerService.GetPlayer(Context.ConnectionId);
            if (name.Trim().Equals(string.Empty))
            {
                await Clients.Client(player.ConnectionId).SendAsync("Logout", "Please pick a name");
                return;
            }

            var otherPlayer = await _playerService.GetPlayerByName(name.Trim());

            if (otherPlayer != null)
            {
                otherPlayer.ConnectionId = Context.ConnectionId;
                otherPlayer.Name = name.Trim();
                await _playerService.SavePlayer(otherPlayer);
                await _playerService.DeletePlayer(player.Id);
            }
            else
            {
                player.Name = name.Trim();
                await _playerService.SavePlayer(player);
            }

            var openGame = await _shmearService.GetOpenGame();
            var seats = await GetSeatsArray(openGame.Id);
            await Clients.Client(player.ConnectionId).SendAsync("ReceiveSeatStatuses", openGame.Id, seats);
        }

        private async Task<string[]> GetSeatsArray(int gameId = 0)
        {
            Game game;
            if (gameId == 0)
            {
                game = await _shmearService.GetOpenGame();
            }
            else
            {
                game = await _shmearService.GetGame(gameId);
            }
            var gamePlayers = await _shmearService.GetGamePlayers(game.Id);
            var seats = new string[] {
                "",
                "",
                "",
                ""
            };

            foreach (var gamePlayer in gamePlayers)
            {
                seats[gamePlayer.SeatNumber - 1] = gamePlayer.Player.Name;
            }
            return seats;
        }

        public async Task<bool> SetSeatStatus(int gameId, int seatNumber)
        {
            if (seatNumber > 0 && seatNumber < 5)
            {
                var game = await _shmearService.GetGame(gameId);
                var player = await _playerService.GetPlayer(Context.ConnectionId);
                var gamePlayers = await _shmearService.GetGamePlayers(game.Id);
                var gamePlayerInSeat = gamePlayers.SingleOrDefault(_ => _.SeatNumber == seatNumber);
                if (gamePlayerInSeat != null)
                {
                    if (gamePlayerInSeat.Player.Id == player.Id)
                        await _shmearService.RemovePlayer(gameId, player.Id);
                    else
                        return false;
                }
                else
                    await _shmearService.AddPlayer(gameId, player.Id, seatNumber);
            }
            else
                throw new ArgumentOutOfRangeException("seatNumber", "Invalid seat number.");

            await SendSeatStatus(gameId);

            await CheckStartGame(gameId);

            return true;
        }

        public async Task SendSeatStatus(int gameId = 0)
        {
            Game game;
            if (gameId == 0)
            {
                game = await _shmearService.GetOpenGame();
            }
            else
            {
                game = await _shmearService.GetGame(gameId);
            }

            var seats = await GetSeatsArray(game.Id);
            await Clients.All.SendAsync("ReceiveSeatStatuses", game.Id, seats);
        }

        public async Task LeaveSeat(int gameId)
        {
            var player = await _playerService.GetPlayer(Context.ConnectionId);
            await _shmearService.RemovePlayer(gameId, player.Id);

            await SendSeatStatus(gameId);
        }

        private async Task CheckStartGame(int gameId)
        {
            var gamePlayers = (await _shmearService.GetGamePlayers(gameId)).ToArray();
            if (gamePlayers.Count() == 4 && await _shmearService.StartGame(gameId))
            {
                var gamePlayerArray = gamePlayers.Select(_ => new string[] { _.SeatNumber.ToString(), _.Player.Name });
                for (int i = 0; i < 4; i++)
                {
                    Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("GamePlayerUpdate", gamePlayerArray);
                }
                await _shmearService.StartRound(gameId);
                await _shmearService.DealCards(gameId);

                await SendCards(gameId);
            }
        }

        private async Task SendCards(int gameId)
        {
            var game = await _shmearService.GetGame(gameId);
            var gamePlayers = (await _shmearService.GetGamePlayers(game.Id)).OrderBy(_ => _.SeatNumber).ToArray();

            for (int i = 0; i < 4; i++)
            {
                var cards = new List<string[]>();
                var hand = await _shmearService.GetHand(game.Id, gamePlayers[i].PlayerId);
                foreach (var handCard in hand)
                {
                    var card = await _shmearService.GetCardAsync(handCard.CardId);
                    cards.Add(new string[] {
                        handCard.CardId.ToString(),
                        card.Value.Name + card.Suit.Name,
                    });
                }
                await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("CardUpdate", cards.ToArray());
            }
            StartWager(gameId);
        }

        public async Task StartWager(int gameId)
        {
            var gamePlayers = (await _shmearService.GetGamePlayers(gameId)).ToArray();
            var nextWagerPlayer = await _shmearService.GetNextWagerGamePlayer(gameId);
            var highestWager = await _shmearService.GetHighestWager(gameId);
            for (var i = 0; i < 4; i++)
            {
                await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("PlayerWagerUpdate", nextWagerPlayer.SeatNumber, highestWager);
            }
        }

        public async Task SetWager(int gameId, int wager)
        {

            var gamePlayers = (await _shmearService.GetGamePlayers(gameId)).ToArray();
            var player = await _playerService.GetPlayer(Context.ConnectionId);
            if (!await _shmearService.SetWager(gameId, player.Id, wager))
            {
                return;
            }

            await SendLog(gameId, player.Name + " wagered " + wager);
            var nextWagerPlayer = await _shmearService.GetNextWagerGamePlayer(gameId);
            if (nextWagerPlayer is null || wager == 5)
            {
                var nextCardPlayer = await _shmearService.GetNextCardGamePlayer(gameId);
                for (var i = 0; i < 4; i++)
                {
                    await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("PlayerTurnUpdate", nextCardPlayer.SeatNumber);
                }
            }
            else
            {
                for (var i = 0; i < 4; i++)
                {
                    await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("PlayerWagerUpdate", nextWagerPlayer.SeatNumber, wager);
                }
            }
        }

        public async Task PlayCard(int gameId, string cardIdString)
        {
            var cardId = int.Parse(cardIdString);
            // var card = await _shmearService.GetCardAsync(cardId);
            // var gamePlayers = (await _shmearService.GetGamePlayers(gameId)).ToArray();
            // var player = await _playerService.GetPlayer(Context.ConnectionId);
            // if (!await _shmearService.PlayCard(gameId, player.Id, cardId))
            // {
            //     return;
            // }

            var card = await _shmearService.GetCardAsync(cardId);
            var gamePlayers = (await _shmearService.GetGamePlayers(gameId)).ToArray();
            var player = await _playerService.GetPlayer(Context.ConnectionId);

            if (gamePlayers.Count(_ => _.Wager != null) == 4 || gamePlayers.Max(_ => (_.Wager ?? 0) == 5))
            {
                player = await _playerService.GetPlayer(Context.ConnectionId);
                var handCards = await _shmearService.GetHand(gameId, player.Id);
                var board = await _shmearService.GetBoardByGameId(gameId);
                var trick = (await _shmearService.GetTricks(gameId)).Single(_ => _.CompletedDate == null);
                var trickCards = await _shmearService.GetTrickCards(trick.Id);
                var currentCardGamePlayer = await _shmearService.GetNextCardGamePlayer(gameId, trick.Id);

                if (currentCardGamePlayer.PlayerId == player.Id
                    && handCards.Any(_ => _.CardId == cardId)
                    && await _shmearService.ValidCardPlay(gameId, board.Id, player.Id, cardId))
                {
                    trick = await _shmearService.PlayCard(trick.Id, player.Id, cardId);
                    await SendLog(gameId, "<p>" + player.Name + " played " + card.Suit.Char + card.Value.Char + "</p>");
                    for (var i = 0; i < 4; i++)
                    {
                        await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("CardPlayed", currentCardGamePlayer.SeatNumber, cardId, card.Value.Name + card.Suit.Name);
                    }

                    // Check to see if the Trick is over
                    trickCards = await _shmearService.GetTrickCards(trick.Id);
                    if (trickCards.Count() == 4)
                    {
                        await EndTrick(gameId, trick);

                        // Check to see if the round is over
                        handCards = await _shmearService.GetHand(gameId, player.Id);
                        if (!handCards.Any())
                        {
                            await EndRound(gameId);
                        }
                        else
                        {
                            trick = await _shmearService.CreateTrick(gameId);
                            var gamePlayer = await _shmearService.GetNextCardGamePlayer(gameId, trick.Id);
                            for (int i = 0; i < 4; i++)
                            {
                                await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("PlayerTurnUpdate", gamePlayer.SeatNumber);
                            }
                        }
                    }
                    else
                    {
                        var gamePlayer = await _shmearService.GetNextCardGamePlayer(gameId, trick.Id);
                        for (int i = 0; i < 4; i++)
                        {
                            await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("PlayerTurnUpdate", gamePlayer.SeatNumber);
                        }
                    }
                }
            }
        }

        private async Task EndTrick(int gameId, Trick trick)
        {
            trick = await _shmearService.EndTrick(trick.Id);
            var winningPlayerId = trick.WinningPlayerId;
            var winningPlayer = await _playerService.GetPlayer((int)winningPlayerId);
            var trickString = new StringBuilder();
            var trickCards = await _shmearService.GetTrickCards(trick.Id);
            foreach (var trickCard in trickCards)
            {
                var cardInTrick = await _shmearService.GetCardAsync(trickCard.CardId);
                trickString.Append(cardInTrick.Suit.Char);
                trickString.Append(cardInTrick.Value.Char);
                trickString.Append(" ");
            }
            await SendLog(gameId, "<p>" + winningPlayer.Name + " won the trick. " + trickString + "</p>");
        }

        private async Task EndRound(int gameId)
        {
            var roundResult = await _shmearService.EndRound(gameId);

            var game = await _shmearService.GetGame(gameId);

            var seatsArray = await GetSeatsArray(gameId);
            var team1Names = $"{seatsArray[0]} and {seatsArray[2]}";
            var team2Names = $"{seatsArray[1]} and {seatsArray[3]}";

            game.Team1Points += roundResult.Team1RoundChange;
            game.Team2Points += roundResult.Team2RoundChange;
            game = await _shmearService.SaveRoundChange(game.Id, game.Team1Points, game.Team2Points);

            string s1 = roundResult.Team1RoundChange == 1 ? "" : "s";
            await SendLog(gameId, string.Format($"<p>Team 1 ({team1Names}) {WagerResult(roundResult, 1)}gained {roundResult.Team1RoundChange} point{s1} ({string.Join(", ", roundResult.Team1PointTypes.Select(_ => _.PointType.ToString() + _.OtherData))}), for a total of {game.Team1Points}</p>"));

            string s2 = roundResult.Team2RoundChange == 1 ? "" : "s";
            await SendLog(gameId, string.Format($"<p>Team 2 ({team2Names}) {WagerResult(roundResult, 2)}gained {roundResult.Team2RoundChange} point{s2} ({string.Join(", ", roundResult.Team2PointTypes.Select(_ => _.PointType.ToString() + _.OtherData))}), for a total of {game.Team2Points}</p>"));

            _shmearService.ClearTricks(gameId);

            if (game.Team1Points >= 11 || game.Team2Points >= 11)
            {
                //var matchResult = await _shmearService.EndMatch(roundResult);
            }

            await _shmearService.StartRound(gameId);
            await _shmearService.DealCards(gameId);
            await SendCards(gameId);
        }

        private async Task EndMatch(Game game)
        {
            if (game.Team1Points >= 11 && game.Team2Points >= 11)
            {
                var wagerGamePlayer = game.GamePlayer.Single(_ => _.Wager == game.GamePlayer.Max(gp => gp.Wager));
                //_shmearService.wagerGamePlayer.SeatNumber
                
            }
        }

        private string WagerResult(RoundResult roundResult, int teamId)
        {
            if (roundResult.TeamWager == teamId)
            {
                return $"with a wager of {roundResult.TeamWagerValue} ";
            }
            return "";
        }

        private async Task SendMessage(string connectionId, string message)
        {
            await Clients.Client(connectionId).SendAsync("SendMessage", message);
        }

        private async Task SendLog(string connectionId, string message)
        {
            await Clients.Client(connectionId).SendAsync("SendLog", message);
        }

        private async Task SendLog(int gameId, string message)
        {
            var gamePlayers = await _shmearService.GetGamePlayers(gameId);
            foreach (var gamePlayer in gamePlayers)
                await SendLog(gamePlayer.Player.ConnectionId, message);
        }

        private async Task SendMessage(int gameId, string message)
        {
            var gamePlayers = await _shmearService.GetGamePlayers(gameId);
            foreach (var gamePlayer in gamePlayers)
                await SendMessage(gamePlayer.Player.ConnectionId, message);
        }

        public async Task SendChat(int gameId, string message)
        {
            var player = await _playerService.GetPlayer(Context.ConnectionId);
            var gamePlayer = await _shmearService.GetGamePlayer(gameId, player.Id);
            if (gamePlayer != null)
                await SendMessage(gameId, $"<b>{gamePlayer.Player.Name}:</b> {message}");
        }
    }
}
