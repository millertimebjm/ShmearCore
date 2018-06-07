using Microsoft.AspNetCore.SignalR;
using Shmear.Business.Services;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Shmear.Business.Models;
//using Shmear.Business.Game;

namespace Shmear.Web.Hubs
{
    public class ShmearHub : Hub
    {
        const string s = "s";
        //private string[] _seats;
        protected static object _seatsLock = new object();

        public async override Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            string userName = Context.User.Identity.Name ?? "";
            string connectionId = Context.ConnectionId;

            var player = await PlayerService.GetPlayer(connectionId);

            if (player == null || player.Id == 0)
            {
                player = new Player()
                {
                    ConnectionId = connectionId,
                    Name = userName
                };
            }

            await PlayerService.SavePlayer(player);
        }

        public async Task SetPlayerName(string name)
        {
            var player = await PlayerService.GetPlayer(Context.ConnectionId);
            if (name.Trim().Equals(string.Empty))
            {
                await Clients.Client(player.ConnectionId).SendAsync("Logout", "Please pick a name");
                return;
            }

            var otherPlayer = await PlayerService.GetPlayerByName(name.Trim());

            if (otherPlayer != null)
            {
                otherPlayer.ConnectionId = Context.ConnectionId;
                otherPlayer.Name = name.Trim();
                await PlayerService.SavePlayer(otherPlayer);
                await PlayerService.DeletePlayer(player.Id);
            }
            else
            {
                player.Name = name.Trim();
                await PlayerService.SavePlayer(player);
            }

            var openGame = await GameService.GetOpenGame();
            var seats = await GetSeatsArray(openGame.Id);
            await Clients.Client(player.ConnectionId).SendAsync("ReceiveSeatStatuses", openGame.Id, seats);
            return;
        }

        private async Task<string[]> GetSeatsArray(int gameId = 0)
        {
            var game = new Game();
            if (gameId == 0)
            {
                game = await GameService.GetOpenGame();
            }
            else
            {
                game = await GameService.GetGame(gameId);
            }
            var gamePlayers = await GameService.GetGamePlayers(game.Id);
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
                var game = await GameService.GetGame(gameId);
                var player = await PlayerService.GetPlayer(Context.ConnectionId);
                var gamePlayers = await GameService.GetGamePlayers(game.Id);
                var gamePlayerInSeat = gamePlayers.SingleOrDefault(_ => _.SeatNumber == seatNumber);
                if (gamePlayerInSeat != null)
                {
                    if (gamePlayerInSeat.Player.Id == player.Id)
                        await GameService.RemovePlayer(gameId, player.Id);
                    else
                        return false;
                }
                else
                    await GameService.AddPlayer(gameId, player.Id, seatNumber);
            }
            else
                throw new Exception("Invalid seat number.");

            await SendSeatStatus(gameId);
            return true;
        }

        public async Task SendSeatStatus(int gameId = 0)
        {
            var game = new Game();
            if (gameId == 0)
            {
                game = await GameService.GetOpenGame();
            }
            else
            {
                game = await GameService.GetGame(gameId);
            }

            var seats = await GetSeatsArray(game.Id);
            await Clients.All.SendAsync("ReceiveSeatStatuses", game.Id, seats);
            //Clients.All.ReceiveSeatStatuses(game.Id, GetSeatsArray(game.Id));
        }

        public async Task LeaveSeat(int gameId)
        {
            var game = await GameService.GetGame(gameId);
            var player = await PlayerService.GetPlayer(Context.ConnectionId);
            //var gamePlayer = GameService.GetGamePlayers(gameId).Single(_ => _.PlayerId == player.Id);
            await GameService.RemovePlayer(gameId, player.Id);

            await SendSeatStatus(gameId);
        }

        public async Task TogglePlayerReadyStatus(int gameId)
        {
            var game = await GameService.GetGame(gameId);
            var player = await PlayerService.GetPlayer(Context.ConnectionId);
            var gamePlayer = await GameService.GetGamePlayer(gameId, player.Id);
            gamePlayer.Ready = !gamePlayer.Ready;
            gamePlayer = await GameService.SaveGamePlayer(gamePlayer);

            await Clients.Client(player.ConnectionId).SendAsync("UpdatePlayerReadyStatus", gamePlayer.Ready);

            await CheckStartGame(gameId);

            //UpdatePlayerReadyStatus(gameId);
        }

        private async Task CheckStartGame(int gameId)
        {
            var gamePlayers = await GameService.GetGamePlayers(gameId);
            if (gamePlayers.Count(_ => _.Ready) == 4)
            {
                if (await GameService.StartGame(gameId))
                {
                    await BoardService.StartRound(gameId);
                    await BoardService.DealCards(gameId);

                    await SendCards(gameId);
                }
            }
        }

        private async Task SendCards(int gameId)
        {
            var game = await GameService.GetGame(gameId);
            var gamePlayers = (await GameService.GetGamePlayers(game.Id)).OrderBy(_ => _.SeatNumber).ToArray();
            var cardCountByPlayerIndex = new int[4];

            for (int i = 0; i < 4; i++)
            {
                var hand = await HandService.GetHand(game.Id, gamePlayers[i].PlayerId);
                cardCountByPlayerIndex[i] = hand.Count();
            }

            for (int i = 0; i < 4; i++)
            {
                var cards = new List<string[]>();
                var hand = await HandService.GetHand(game.Id, gamePlayers[i].PlayerId);
                foreach (var handCard in hand)
                {
                    var card = await CardService.GetCardAsync(handCard.CardId);
                    cards.Add(new string[] {
                        handCard.CardId.ToString(),
                        //card.Suit.Char.ToString() + card.Value.Char.ToString()
                        card.Value.Name + card.Suit.Name,
                    });
                }
                await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("CardUpdate", i, cards.ToArray(), cardCountByPlayerIndex);
            }

            gamePlayers = (await GameService.GetGamePlayers(gameId)).ToArray();
            if (gamePlayers.Count(_ => _.Wager != null) == 4 || gamePlayers.Max(_ => (_.Wager ?? 0) == 5))
            {
                var trick = (await TrickService.GetTricks(gameId)).SingleOrDefault(_ => _.CompletedDate == null);
                if (trick == null)
                {
                    trick = await TrickService.CreateTrick(gameId);
                }
                var gamePlayer = await BoardService.GetNextCardPlayer(gameId, trick.Id);
                for (var i = 0; i < 4; i++)
                {
                    await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("HideWager");
                    //await Clients.Client(gamePlayers[i].Player.ConnectionId).hideWager();
                    await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("PlayerTurnUpdate", gamePlayer.SeatNumber);
                    //Clients.Client(gamePlayers[i].Player.ConnectionId).playerTurnUpdate(gamePlayer.SeatNumber);
                }
            }
            else
            {
                var gamePlayer = await BoardService.GetNextWagerPlayer(gameId);
                var highestWager = gamePlayers.Max(_ => _.Wager ?? 0);

                for (var i = 0; i < 4; i++)
                {
                    await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("PlayerTurnUpdate", gamePlayer.SeatNumber);
                    //Clients.Client(gamePlayers[i].Player.ConnectionId).playerTurnUpdate(gamePlayer.SeatNumber);
                    await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("WagerUpdate", highestWager);
                    //Clients.Client(gamePlayers[i].Player.ConnectionId).wagerUpdate(highestWager);
                }
            }
        }

        public async Task SetWager(int gameId, int wager)
        {
            var gamePlayers = (await GameService.GetGamePlayers(gameId)).ToArray();
            var nextGamePlayer = await BoardService.GetNextWagerPlayer(gameId);
            var player = await PlayerService.GetPlayer(Context.ConnectionId);
            if (nextGamePlayer.PlayerId == player.Id && gamePlayers.Select(_ => _.PlayerId).Contains(player.Id))
            {
                await BoardService.SetWager(gameId, player.Id, wager);

                var board = await BoardService.GetBoardByGameId(gameId);
                gamePlayers = (await GameService.GetGamePlayers(gameId)).OrderBy(_ => _.SeatNumber).ToArray();

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

                    await BoardService.SaveBoard(board);
                }

                await SendCards(gameId);
                SendMessage(gameId, player.Name + " wagered " + wager);
            }
        }

        public async Task PlayCard(int gameId, int cardId)
        {
            var card = await CardService.GetCardAsync(cardId);
            var gamePlayers = await GameService.GetGamePlayers(gameId);
            if (gamePlayers.Count(_ => _.Wager != null) == 4 || gamePlayers.Max(_ => (_.Wager ?? 0) == 5))
            {
                var player = await PlayerService.GetPlayer(Context.ConnectionId);
                var handCards = await HandService.GetHand(gameId, player.Id);
                var board = await BoardService.GetBoardByGameId(gameId);
                var tricks = (await TrickService.GetTricks(gameId)).Single(_ => _.CompletedDate == null);
                var trick = await TrickService.GetTrick(tricks.Id);
                var nextCardGamePlayer = await BoardService.GetNextCardPlayer(gameId, trick.Id);

                if (nextCardGamePlayer.PlayerId == player.Id
                    && handCards.Any(_ => _.CardId == cardId)
                    && await GameService.ValidCardPlay(gameId, board.Id, player.Id, cardId))
                {
                    trick = await TrickService.PlayCard(trick.Id, player.Id, cardId);
                    await SendMessage(gameId, "<p>" + player.Name + " played " + card.Suit.Char + card.Value.Char + "</p>");

                    // Check to see if the Trick is over
                    if (trick.TrickCard.Count() == 4)
                    {
                        await EndTrick(gameId, trick);

                        // Check to see if the round is over
                        handCards = await HandService.GetHand(gameId, player.Id);
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
            trick = await TrickService.EndTrick(trick.Id);
            var winningPlayerId = trick.WinningPlayerId;
            var winningPlayer = await PlayerService.GetPlayer((int)winningPlayerId);
            var trickString = string.Empty;
            foreach (var trickCard in trick.TrickCard)
            {
                var cardInTrick = await CardService.GetCardAsync(trickCard.CardId);
                trickString += cardInTrick.Suit.Char + cardInTrick.Value.Char + " ";
            }
            await SendMessage(gameId, "<p>" + winningPlayer.Name + " won the trick. " + trickString + "</p>");
        }

        private async Task EndRound(int gameId)
        {
            var roundResult = await BoardService.EndRound(gameId);

            var game = await GameService.GetGame(gameId);
            game.Team1Points += roundResult.Team1RoundChange;
            game.Team2Points += roundResult.Team2RoundChange;
            await GameService.SaveGame(game);

            string s1 = roundResult.Team1RoundChange == 1 ? "" : "s";
            await SendMessage(gameId, string.Format($"<p>Team 1 {WagerResult(roundResult, 1)}gained {roundResult.Team1RoundChange} point{s1} ({string.Join(", ", roundResult.Team1Points.Select(_ => _.PointType.ToString() + _.OtherData))}), for a total of {game.Team1Points}</p>"));

            string s2 = roundResult.Team2RoundChange == 1 ? "" : "s";
            await SendMessage(gameId, string.Format($"<p>Team 2 {WagerResult(roundResult, 2)}gained {roundResult.Team2RoundChange} point{s2} ({string.Join(", ", roundResult.Team2Points.Select(_ => _.PointType.ToString() + _.OtherData))}), for a total of {game.Team2Points}</p>"));

            TrickService.ClearTricks(gameId);
            await BoardService.StartRound(gameId);
            await BoardService.DealCards(gameId);
        }

        private string WagerResult(RoundResult roundResult, int teamId)
        {
            if (roundResult.TeamWager == teamId)
            {
                return $"with a wager of {roundResult.TeamWagerValue} ";
            }
            return "";
        }

        private async Task SendMessage(int gameId, int playerId, string message)
        {
            var gamePlayer = await GameService.GetGamePlayer(gameId, playerId);
            await SendMessage(gameId, gamePlayer.Player.ConnectionId, message);
        }

        private async Task SendMessage(int gameId, string connectionId, string message)
        {
            await Clients.Client(connectionId).SendAsync("SendMessage", message);
        }

        private async Task SendMessage(int gameId, string message)
        {
            var gamePlayers = await GameService.GetGamePlayers(gameId);
            foreach (var gamePlayer in gamePlayers)
            {
                await SendMessage(gameId, gamePlayer.Player.ConnectionId, message);
            }
        }

        public async Task SendChat(int gameId, string message)
        {
            var player = await PlayerService.GetPlayer(Context.ConnectionId);
            var gamePlayer = await GameService.GetGamePlayer(gameId, player.Id);
            if (gamePlayer != null)
                await SendMessage(gameId, $"<b>{gamePlayer.Player.Name}:</b> {message}");
        }

        //private void CheckStartGame(int gameId)
        //{
        //    var gamePlayers = GameService.GetGamePlayers(gameId);
        //    if (gamePlayers.Count(_ => _.Ready) == 4)
        //    {
        //        if (GameService.StartGame(gameId))
        //        {
        //            BoardService.StartRound(gameId);
        //            BoardService.DealCards(gameId);

        //            SendCards(gameId);
        //        }
        //    }
        //}


        //public override Task OnConnected()
        //{
        //    string userName = Context.User.Identity.Name;
        //    string connectionId = Context.ConnectionId;

        //    var player = PlayerService.GetPlayer(connectionId);

        //    if (player == null || player.Id == 0)
        //    {
        //        player = new Player()
        //        {
        //            ConnectionId = connectionId,
        //            Name = userName
        //        };
        //    }

        //    PlayerService.SavePlayer(player);

        //    return base.OnConnected();
        //}





        //private void SendMessage(int gameId, int playerId, string message)
        //{
        //    var gamePlayer = GameService.GetGamePlayer(gameId, playerId);
        //    SendMessage(gameId, gamePlayer.Player.ConnectionId, message);
        //}

        //private void SendMessage(int gameId, string connectionId, string message)
        //{
        //    Clients.Client(connectionId).sendMessage(message);
        //}
    }
}
