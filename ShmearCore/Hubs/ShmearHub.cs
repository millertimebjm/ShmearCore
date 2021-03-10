using Microsoft.AspNetCore.SignalR;
using Shmear.Business.Services;
using Shmear.EntityFramework.EntityFrameworkCore.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Shmear.Business.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Shmear.EntityFramework.EntityFrameworkCore;

namespace Shmear.Web.Hubs
{
    public class ShmearHub : Hub
    {
        private readonly DbContextOptions<CardContext> options;

        public ShmearHub()
            : base()
        {
            var optionsBuilder = new DbContextOptionsBuilder<CardContext>();
            //optionsBuilder.UseSqlServer(@"Server=localhost;Database=Card.Dev;Trusted_Connection=True;");
            optionsBuilder.UseNpgsql("Host=localhost;Database=Card.Dev;Username=postgres;Password=M8WQn8*Nz%gQEc");
            options = optionsBuilder.Options;
        }

        public async override Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            
            string userName = Context.User.Identity.Name ?? "";
            string connectionId = Context.ConnectionId;

            var player = await PlayerService.GetPlayer(options, connectionId);

            if (player == null || player.Id == 0)
            {
                player = new Player()
                {
                    ConnectionId = connectionId,
                    Name = userName
                };
            }

            await PlayerService.SavePlayer(options, player);
        }

        public async Task SetPlayerName(string name)
        {
            var player = await PlayerService.GetPlayer(options, Context.ConnectionId);
            if (name.Trim().Equals(string.Empty))
            {
                await Clients.Client(player.ConnectionId).SendAsync("Logout", "Please pick a name");
                return;
            }

            var otherPlayer = await PlayerService.GetPlayerByName(options, name.Trim());

            if (otherPlayer != null)
            {
                otherPlayer.ConnectionId = Context.ConnectionId;
                otherPlayer.Name = name.Trim();
                await PlayerService.SavePlayer(options, otherPlayer);
                await PlayerService.DeletePlayer(options, player.Id);
            }
            else
            {
                player.Name = name.Trim();
                await PlayerService.SavePlayer(options, player);
            }

            var openGame = await GameService.GetOpenGame(options);
            var seats = await GetSeatsArray(openGame.Id);
            await Clients.Client(player.ConnectionId).SendAsync("ReceiveSeatStatuses", openGame.Id, seats);
        }

        private async Task<string[]> GetSeatsArray(int gameId = 0)
        {
            Game game;
            if (gameId == 0)
            {
                game = await GameService.GetOpenGame(options);
            }
            else
            {
                game = await GameService.GetGame(options, gameId);
            }
            var gamePlayers = await GameService.GetGamePlayers(options, game.Id);
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
                var game = await GameService.GetGame(options, gameId);
                var player = await PlayerService.GetPlayer(options, Context.ConnectionId);
                var gamePlayers = await GameService.GetGamePlayers(options, game.Id);
                var gamePlayerInSeat = gamePlayers.SingleOrDefault(_ => _.SeatNumber == seatNumber);
                if (gamePlayerInSeat != null)
                {
                    if (gamePlayerInSeat.Player.Id == player.Id)
                        await GameService.RemovePlayer(options, gameId, player.Id);
                    else
                        return false;
                }
                else
                    await GameService.AddPlayer(options, gameId, player.Id, seatNumber);
            }
            else
                throw new ArgumentOutOfRangeException("seatNumber", "Invalid seat number.");

            await SendSeatStatus(gameId);
            return true;
        }

        public async Task SendSeatStatus(int gameId = 0)
        {
            Game game;
            if (gameId == 0)
            {
                game = await GameService.GetOpenGame(options);
            }
            else
            {
                game = await GameService.GetGame(options, gameId);
            }

            var seats = await GetSeatsArray(game.Id);
            await Clients.All.SendAsync("ReceiveSeatStatuses", game.Id, seats);
        }

        public async Task LeaveSeat(int gameId)
        {
            var player = await PlayerService.GetPlayer(options, Context.ConnectionId);
            await GameService.RemovePlayer(options, gameId, player.Id);

            await SendSeatStatus(gameId);
        }

        public async Task TogglePlayerReadyStatus(int gameId)
        {
            var player = await PlayerService.GetPlayer(options, Context.ConnectionId);
            var gamePlayer = await GameService.GetGamePlayer(options, gameId, player.Id);
            gamePlayer.Ready = !gamePlayer.Ready;
            gamePlayer = await GameService.SaveGamePlayer(options, gamePlayer);

            await Clients.Client(player.ConnectionId).SendAsync("UpdatePlayerReadyStatus", gamePlayer.Ready);

            await CheckStartGame(gameId);
        }

        private async Task CheckStartGame(int gameId)
        {
            var gamePlayers = await GameService.GetGamePlayers(options, gameId);
            if (gamePlayers.Count(_ => _.Ready) == 4 && await GameService.StartGame(options, gameId))
            {
                await BoardService.StartRound(options, gameId);
                await BoardService.DealCards(options, gameId);

                await SendCards(gameId);
            }
        }

        private async Task SendCards(int gameId)
        {
            var game = await GameService.GetGame(options, gameId);
            var gamePlayers = (await GameService.GetGamePlayers(options, game.Id)).OrderBy(_ => _.SeatNumber).ToArray();
            var cardCountByPlayerIndex = new int[4];

            for (int i = 0; i < 4; i++)
            {
                var hand = await HandService.GetHand(options, game.Id, gamePlayers[i].PlayerId);
                cardCountByPlayerIndex[i] = hand.Count();
            }

            for (int i = 0; i < 4; i++)
            {
                var cards = new List<string[]>();
                var hand = await HandService.GetHand(options, game.Id, gamePlayers[i].PlayerId);
                foreach (var handCard in hand)
                {
                    var card = await CardService.GetCardAsync(options, handCard.CardId);
                    cards.Add(new string[] {
                        handCard.CardId.ToString(),
                        card.Value.Name + card.Suit.Name,
                    });
                }
                await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("CardUpdate", i, cards.ToArray(), cardCountByPlayerIndex);
            }

            gamePlayers = (await GameService.GetGamePlayers(options, gameId)).ToArray();
            if (gamePlayers.Count(_ => _.Wager != null) == 4 || gamePlayers.Max(_ => (_.Wager ?? 0) == 5))
            {
                var trick = (await TrickService.GetTricks(options, gameId)).SingleOrDefault(_ => _.CompletedDate == null);
                if (trick == null)
                {
                    trick = await TrickService.CreateTrick(options, gameId);
                }
                var gamePlayer = await BoardService.GetNextCardPlayer(options, gameId, trick.Id);
                for (var i = 0; i < 4; i++)
                {
                    await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("HideWager");
                    await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("PlayerTurnUpdate", gamePlayer.SeatNumber);
                }
            }
            else
            {
                var gamePlayer = await BoardService.GetNextWagerPlayer(options, gameId);
                var highestWager = gamePlayers.Max(_ => _.Wager ?? 0);

                for (var i = 0; i < 4; i++)
                {
                    await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("PlayerTurnUpdate", gamePlayer.SeatNumber);
                    await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("WagerUpdate", highestWager);
                }
            }
        }

        public async Task SetWager(int gameId, int wager)
        {
            var gamePlayers = (await GameService.GetGamePlayers(options, gameId)).ToArray();
            var nextGamePlayer = await BoardService.GetNextWagerPlayer(options, gameId);
            var player = await PlayerService.GetPlayer(options, Context.ConnectionId);
            if (nextGamePlayer.PlayerId == player.Id && gamePlayers.Select(_ => _.PlayerId).Contains(player.Id))
            {
                await BoardService.SetWager(options, gameId, player.Id, wager);

                await SendCards(gameId);
                await SendMessage(gameId, player.Name + " wagered " + wager);
            }
        }

        public async Task PlayCard(int gameId, int cardId)
        {
            var card = await CardService.GetCardAsync(options, cardId);
            var gamePlayers = await GameService.GetGamePlayers(options, gameId);
            if (gamePlayers.Count(_ => _.Wager != null) == 4 || gamePlayers.Max(_ => (_.Wager ?? 0) == 5))
            {
                var player = await PlayerService.GetPlayer(options, Context.ConnectionId);
                var handCards = await HandService.GetHand(options, gameId, player.Id);
                var board = await BoardService.GetBoardByGameId(options, gameId);
                var trick = (await TrickService.GetTricks(options, gameId)).Single(_ => _.CompletedDate == null);
                var trickCards = await TrickService.GetTrickCards(options, trick.Id);
                var nextCardGamePlayer = await BoardService.GetNextCardPlayer(options, gameId, trick.Id);

                if (nextCardGamePlayer.PlayerId == player.Id
                    && handCards.Any(_ => _.CardId == cardId)
                    && await GameService.ValidCardPlay(options, gameId, board.Id, player.Id, cardId))
                {
                    trick = await TrickService.PlayCard(options, trick.Id, player.Id, cardId);
                    await SendMessage(gameId, "<p>" + player.Name + " played " + card.Suit.Char + card.Value.Char + "</p>");

                    // Check to see if the Trick is over
                    trickCards = await TrickService.GetTrickCards(options, trick.Id);
                    if (trickCards.Count() == 4)
                    {
                        await EndTrick(gameId, trick);

                        // Check to see if the round is over
                        handCards = await HandService.GetHand(options, gameId, player.Id);
                        if (!handCards.Any())
                        {
                            await EndRound(gameId);
                        }
                    }

                    await SendCards(gameId);
                }
            }
        }

        private async Task EndTrick(int gameId, Trick trick)
        {
            trick = await TrickService.EndTrick(options, trick.Id);
            var winningPlayerId = trick.WinningPlayerId;
            var winningPlayer = await PlayerService.GetPlayer(options, (int)winningPlayerId);
            var trickString = new StringBuilder();
            var trickCards = await TrickService.GetTrickCards(options, trick.Id);
            foreach (var trickCard in trickCards)
            {
                var cardInTrick = await CardService.GetCardAsync(options, trickCard.CardId);
                trickString.Append(cardInTrick.Suit.Char);
                trickString.Append(cardInTrick.Value.Char);
                trickString.Append(" ");
            }
            await SendMessage(gameId, "<p>" + winningPlayer.Name + " won the trick. " + trickString + "</p>");
        }

        private async Task EndRound(int gameId)
        {
            var roundResult = await BoardService.EndRound(options, gameId);

            var game = await GameService.GetGame(options, gameId);
            game.Team1Points += roundResult.Team1RoundChange;
            game.Team2Points += roundResult.Team2RoundChange;
            game = await GameService.SaveRoundChange(options, game.Id, game.Team1Points, game.Team2Points);

            string s1 = roundResult.Team1RoundChange == 1 ? "" : "s";
            await SendMessage(gameId, string.Format($"<p>Team 1 {WagerResult(roundResult, 1)}gained {roundResult.Team1RoundChange} point{s1} ({string.Join(", ", roundResult.Team1Points.Select(_ => _.PointType.ToString() + _.OtherData))}), for a total of {game.Team1Points}</p>"));

            string s2 = roundResult.Team2RoundChange == 1 ? "" : "s";
            await SendMessage(gameId, string.Format($"<p>Team 2 {WagerResult(roundResult, 2)}gained {roundResult.Team2RoundChange} point{s2} ({string.Join(", ", roundResult.Team2Points.Select(_ => _.PointType.ToString() + _.OtherData))}), for a total of {game.Team2Points}</p>"));

            TrickService.ClearTricks(options, gameId);
            await BoardService.StartRound(options, gameId);
            await BoardService.DealCards(options, gameId);
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

        private async Task SendMessage(int gameId, string message)
        {
            var gamePlayers = await GameService.GetGamePlayers(options, gameId);
            foreach (var gamePlayer in gamePlayers)
                await SendMessage(gamePlayer.Player.ConnectionId, message);
            
        }

        public async Task SendChat(int gameId, string message)
        {
            var player = await PlayerService.GetPlayer(options, Context.ConnectionId);
            var gamePlayer = await GameService.GetGamePlayer(options, gameId, player.Id);
            if (gamePlayer != null)
                await SendMessage(gameId, $"<b>{gamePlayer.Player.Name}:</b> {message}");
        }
    }
}
