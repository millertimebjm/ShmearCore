﻿using Microsoft.AspNetCore.SignalR;
using Shmear.Business.Services;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System.Threading.Tasks;
//using Shmear.Business.Game;

namespace Shmear.Web.Hubs
{
    public class ShmearHub : Hub
    {
        const string s = "s";
        private string[] _seats;
        protected static object _seatsLock = new object();

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        //public async override Task OnConnectedAsync()
        //{
        //    string userName = Context.User.Identity.Name;
        //    string connectionId = Context.ConnectionId;

        //    var player = await PlayerService.GetPlayer(connectionId);

        //    if (player == null || player.Id == 0)
        //    {
        //        player = new Player()
        //        {
        //            ConnectionId = connectionId,
        //            Name = userName
        //        };
        //    }

        //    await PlayerService.SavePlayer(player);

        //    return base.OnConnectedAsync();
        //}

        //public async Task SetPlayerName(string name)
        //{
        //    var player = await PlayerService.GetPlayer(Context.ConnectionId);
        //    if (name.Trim().Equals(string.Empty))
        //    {
        //        Clients.Client(player.ConnectionId).LogoutPlayer("Please pick a name");
        //        return;
        //    }

        //    var otherPlayer = await PlayerService.GetPlayerByName(name.Trim());

        //    if (otherPlayer != null)
        //    {
        //        otherPlayer.ConnectionId = Context.ConnectionId;
        //        otherPlayer.Name = name.Trim();
        //        await PlayerService.SavePlayer(otherPlayer);
        //        await PlayerService.DeletePlayer(player.Id);
        //    }
        //    else
        //    {
        //        player.Name = name.Trim();
        //        PlayerService.SavePlayer(player);
        //    }

        //    var openGame = await GameService.GetOpenGame();
        //    Clients.Client(player.ConnectionId).ReceiveSeatStatuses(openGame.Id, GetSeatsArray(openGame.Id));
        //    return;
        //}

        //private async string[] GetSeatsArray(int gameId = 0)
        //{
        //    var game = new Game();
        //    if (gameId == 0)
        //    {
        //        game = await GameService.GetOpenGame();
        //    }
        //    else
        //    {
        //        game = await GameService.GetGame(gameId);
        //    }
        //    var gamePlayers = await GameService.GetGamePlayers(game.Id);
        //    var seats = new string[] {
        //        "",
        //        "",
        //        "",
        //        ""
        //    };

        //    foreach (var gamePlayer in gamePlayers)
        //    {
        //        seats[gamePlayer.SeatNumber - 1] = gamePlayer.Player.Name;
        //    }
        //    return seats;
        //}
    }
}
