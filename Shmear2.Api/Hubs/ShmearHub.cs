using Microsoft.AspNetCore.SignalR;
using System.Text;
using Shmear2.Business.Services;
using Shmear2.Business.Database.Models;
using Shmear2.Business.Models;
using Shmear2.Business.Database;

namespace Shmear2.Api.Hubs
{
    public class ShmearHub : Hub
    {
        private readonly CardDbContext _cardDb;
        private readonly IPlayerService _playerService;
        private readonly IShmearService _shmearService;
        private readonly IPlayerComputerService _playerComputerService;

        public ShmearHub(
            CardDbContext cardDb,
            IPlayerService playerService,
            IShmearService shmearService,
            IPlayerComputerService playerComputerService
        )
            : base()
        {
            _cardDb = cardDb;
            _playerService = playerService;
            _shmearService = shmearService;
            _playerComputerService = playerComputerService;
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

        public async Task<bool> SetComputerSeatStatus(int gameId, int seatNumber)
        {
            if (seatNumber > 0 && seatNumber < 5)
            {
                var game = await _shmearService.GetGame(gameId);
                //var player = await _playerService.GetPlayer(Context.ConnectionId);
                var randomService = new Random();
                var player = await _playerService.SavePlayer(
                    new Player()
                    {
                        Id = 0,
                        IsComputer = true,
                        Name = "Comp" + randomService.Next(1000, 9999),
                    }
                );
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
            var gamePlayers = await _shmearService.GetHumanGamePlayers(gameId);
            await Clients.All.SendAsync("ReceiveSeatStatuses", game.Id, seats);
            // foreach (var gamePlayer in gamePlayers)
            // {
            //     await Clients
            //         .Client(gamePlayer.Player.ConnectionId)
            //         .SendAsync("ReceiveSeatStatuses", game.Id, seats);
            // }
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
                var humanGamePlayers = await _shmearService.GetHumanGamePlayers(gameId);
                foreach (var humanGamePlayer in humanGamePlayers)
                {
                    await Clients.Client(humanGamePlayer.Player.ConnectionId).SendAsync("GamePlayerUpdate", gamePlayerArray);
                }
                await _shmearService.StartRound(gameId);
                await _shmearService.DealCards(gameId);

                await SendCards(gameId);
            }
        }

        private async Task SendCards(int gameId)
        {
            var game = await _shmearService.GetGame(gameId);
            //var gamePlayers = (await _shmearService.GetGamePlayers(game.Id)).OrderBy(_ => _.SeatNumber).ToArray();
            var humanGamePlayers = await _shmearService.GetHumanGamePlayers(gameId);
            foreach (var humanGamePlayer in humanGamePlayers)
            {
                var cards = new List<string[]>();
                var hand = await _shmearService.GetHand(game.Id, humanGamePlayer.PlayerId);
                foreach (var handCard in hand)
                {
                    var card = await _shmearService.GetCardAsync(handCard.CardId);
                    cards.Add(new string[] {
                        handCard.CardId.ToString(),
                        card.Value.Name + card.Suit.Name,
                    });
                }
                await Clients.Client(humanGamePlayer.Player.ConnectionId).SendAsync("CardUpdate", cards.ToArray());
            }
            // for (int i = 0; i < 4; i++)
            // {
            //     var cards = new List<string[]>();
            //     var hand = await _shmearService.GetHand(game.Id, gamePlayers[i].PlayerId);
            //     foreach (var handCard in hand)
            //     {
            //         var card = await _shmearService.GetCardAsync(handCard.CardId);
            //         cards.Add(new string[] {
            //             handCard.CardId.ToString(),
            //             card.Value.Name + card.Suit.Name,
            //         });
            //     }
            //     await Clients.Client(gamePlayers[i].Player.ConnectionId).SendAsync("CardUpdate", cards.ToArray());
            // }
            await StartWager(gameId);
        }

        public async Task StartWager(int gameId)
        {
            var nextWagerPlayer = await _shmearService.GetNextWagerGamePlayer(gameId);
            var highestWager = await _shmearService.GetHighestWager(gameId);
            // for (var i = 0; i < 4; i++)
            var humanGamePlayers = await _shmearService.GetHumanGamePlayers(gameId);
            foreach (var humanGamePlayer in humanGamePlayers)
            {
                await Clients.Client(humanGamePlayer.Player.ConnectionId).SendAsync("PlayerWagerUpdate", nextWagerPlayer.SeatNumber, highestWager);
            }

            if (nextWagerPlayer.Player.ConnectionId is null)
            {
                Thread.Sleep(1000);
                var computerWager = await _playerComputerService.SetWager(gameId, nextWagerPlayer.Id);
                await SetWagerInternal(gameId, computerWager, nextWagerPlayer.Id);
            }
        }

        public async Task SetWager(int gameId, int wager)
        {
            var player = await _playerService.GetPlayer(Context.ConnectionId);
            var gamePlayer = await _shmearService.GetGamePlayer(gameId, player.Id);
            await SetWagerInternal(gameId, wager, gamePlayer.Id);
        }

        public async Task SetWagerInternal(int gameId, int wager, int gamePlayerId)
        {
            var gamePlayer = await _shmearService.GetGamePlayer(gamePlayerId);
            if (!await _shmearService.SetWager(gameId, gamePlayer.PlayerId, wager))
            {
                return;
            }

            await SendLog(gameId, gamePlayer.Player.Name + " wagered " + wager);
            var nextWagerPlayer = await _shmearService.GetNextWagerGamePlayer(gameId);
            var humanGamePlayers = await _shmearService.GetHumanGamePlayers(gameId);
            if (nextWagerPlayer is null || wager == 5)
            {
                var nextCardPlayer = await _shmearService.GetNextCardGamePlayer(gameId);
                foreach (var humanGamePlayer in humanGamePlayers)
                {
                    await Clients.Client(humanGamePlayer.Player.ConnectionId).SendAsync("PlayerTurnUpdate", nextCardPlayer.SeatNumber);
                }
            }
            else
            {
                foreach (var humanGamePlayer in humanGamePlayers)
                {
                    await Clients.Client(humanGamePlayer.Player.ConnectionId).SendAsync("PlayerWagerUpdate", nextWagerPlayer.SeatNumber, wager);
                }
            }

            if (nextWagerPlayer != null && nextWagerPlayer.Player.ConnectionId is null)
            {
                Thread.Sleep(1000);
                var computerWager = await _playerComputerService.SetWager(gameId, nextWagerPlayer.Id);
                await SetWagerInternal(gameId, computerWager, nextWagerPlayer.Id);
            }
        }

        public async Task PlayCard(int gameId, int cardId)
        {
            var player = await _playerService.GetPlayer(Context.ConnectionId);
            var gamePlayer = await _shmearService.GetGamePlayer(gameId, player.Id);
            await PlayCardInternal(gameId, cardId, gamePlayer.Id);
        }

        public async Task PlayCardInternal(int gameId, int cardId, int gamePlayerId)
        {
            // var card = await _shmearService.GetCardAsync(cardId);
            // var gamePlayers = (await _shmearService.GetGamePlayers(gameId)).ToArray();
            // var player = await _playerService.GetPlayer(Context.ConnectionId);
            // if (!await _shmearService.PlayCard(gameId, player.Id, cardId))
            // {
            //     return;
            // }
            var gamePlayer = await _shmearService.GetGamePlayer(gamePlayerId);
            var card = await _shmearService.GetCardAsync(cardId);
            var gamePlayers = (await _shmearService.GetGamePlayers(gameId)).ToArray();

            if (gamePlayers.Count(_ => _.Wager != null) == 4 || gamePlayers.Max(_ => (_.Wager ?? 0) == 5))
            {
                var handCards = await _shmearService.GetHand(gameId, gamePlayer.PlayerId);
                var board = await _shmearService.GetBoardByGameId(gameId);
                var trick = (await _shmearService.GetTricks(gameId)).Single(_ => _.CompletedDate == null);
                var trickCards = await _shmearService.GetTrickCards(trick.Id);
                var currentCardGamePlayer = await _shmearService.GetNextCardGamePlayer(gameId, trick.Id);

                if (currentCardGamePlayer.PlayerId == gamePlayer.PlayerId
                    && handCards.Any(_ => _.CardId == cardId)
                    && await _shmearService.ValidCardPlay(gameId, board.Id, gamePlayer.PlayerId, cardId))
                {
                    trick = await _shmearService.PlayCard(trick.Id, gamePlayer.PlayerId, cardId);
                    await SendLog(gameId, "<p>" + gamePlayer.Player.Name + " played " + card.Suit.Char + card.Value.Char + "</p>");
                    var humanGamePlayers = await _shmearService.GetHumanGamePlayers(gameId);
                    foreach (var humanGamePlayer in humanGamePlayers)
                    {
                        await Clients.Client(humanGamePlayer.Player.ConnectionId).SendAsync("CardPlayed", currentCardGamePlayer.SeatNumber, cardId, card.Value.Name + card.Suit.Name);
                    }

                    // Check to see if the Trick is over
                    trickCards = await _shmearService.GetTrickCards(trick.Id);
                    if (trickCards.Count() == 4)
                    {
                        await EndTrick(gameId, trick);

                        // Check to see if the round is over
                        handCards = await _shmearService.GetHand(gameId, gamePlayer.PlayerId);
                        if (!handCards.Any())
                        {
                            await EndRound(gameId);
                        }
                        else
                        {
                            trick = await _shmearService.CreateTrick(gameId);
                            gamePlayer = await _shmearService.GetNextCardGamePlayer(gameId, trick.Id);
                            foreach (var humanGamePlayer in humanGamePlayers)
                            {
                                await Clients.Client(humanGamePlayer.Player.ConnectionId).SendAsync("PlayerTurnUpdate", gamePlayer.SeatNumber);
                            }
                            if (gamePlayer != null && gamePlayer.Player.ConnectionId is null)
                            {
                                Thread.Sleep(1000);
                                var computerCardId = await _playerComputerService.PlayCard(gameId, gamePlayer.Id);
                                await PlayCardInternal(gameId, computerCardId, gamePlayer.Id);
                            }
                        }
                    }
                    else
                    {
                        gamePlayer = await _shmearService.GetNextCardGamePlayer(gameId, trick.Id);
                        foreach (var humanGamePlayer in humanGamePlayers)
                        {
                            await Clients.Client(humanGamePlayer.Player.ConnectionId).SendAsync("PlayerTurnUpdate", gamePlayer.SeatNumber);
                        }
                        if (gamePlayer != null && gamePlayer.Player.ConnectionId is null)
                        {
                            Thread.Sleep(1000);
                            var computerCardId = await _playerComputerService.PlayCard(gameId, gamePlayer.Id);
                            await PlayCardInternal(gameId, computerCardId, gamePlayer.Id);
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

            if (roundResult.Team1Points >= 11 || roundResult.Team2Points >= 11)
            {
                var matchResult = await _shmearService.EndMatch(gameId, roundResult);
                await SendLog(gameId, $"Team {matchResult.TeamMatchWinner} won the match.  Team 1 ({team1Names}) has {matchResult.Team1Matches}.  Team 2 ({team2Names}) has {matchResult.Team2Matches}");
            }


            await _shmearService.StartRound(gameId);
            await _shmearService.DealCards(gameId);
            await SendCards(gameId);
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
            var humanGamePlayers = await _shmearService.GetHumanGamePlayers(gameId);
            foreach (var humanGamePlayer in humanGamePlayers)
                await SendLog(humanGamePlayer.Player.ConnectionId, message);
        }

        private async Task SendMessage(int gameId, string message)
        {
            var humanGamePlayers = await _shmearService.GetHumanGamePlayers(gameId);
            foreach (var humanGamePlayer in humanGamePlayers)
                await SendMessage(humanGamePlayer.Player.ConnectionId, message);
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
