using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shmear.Business.Services
{
    public class PlayerComputerService
    {
        public double Aggressiveness = 50.0d;
        public int PointsToWinRound = 11;
        public Dictionary<CardService.ValueEnum, double> valueWorth;

        public PlayerComputerService()
        {
            valueWorth = new Dictionary<CardService.ValueEnum, double>();
            valueWorth.Add(CardService.ValueEnum.Ace, 2.0);
            valueWorth.Add(CardService.ValueEnum.King, 1.6);
            valueWorth.Add(CardService.ValueEnum.Queen, 1.3);
            valueWorth.Add(CardService.ValueEnum.Jack, 1.0);
            valueWorth.Add(CardService.ValueEnum.Joker, 0.9);
            valueWorth.Add(CardService.ValueEnum.Ten, 0.7);
            valueWorth.Add(CardService.ValueEnum.Nine, 0.4);
            valueWorth.Add(CardService.ValueEnum.Eight, 0.3);
            valueWorth.Add(CardService.ValueEnum.Seven, 1.0);
        }

        public async Task<KeyValuePair<CardService.SuitEnum, double>> Wager(DbContextOptions<CardContext> options, int gameId, int playerId)
        {
            var game = await GameService.GetGame(options, gameId);
            var gamePlayer = await GameService.GetGamePlayer(options, gameId, playerId);
            SetAggressiveness(gamePlayer.SeatNumber, game.Team1Points, game.Team2Points);
            var suitWorth = await CalculateHandValuePerSuit(options, gameId, playerId);
            var bestSuit = suitWorth.Keys.Single(_ => suitWorth[_] == suitWorth.Values.Max());
            var bestWorth = suitWorth[bestSuit];

            return new KeyValuePair<CardService.SuitEnum, double>(bestSuit, bestWorth);
        }

        private async Task<Dictionary<CardService.SuitEnum, double>> CalculateHandValuePerSuit(DbContextOptions<CardContext> options, int gameId, int playerId)
        {
            var handCards = await HandService.GetHand(options, gameId, playerId);
            var dictionary = new Dictionary<CardService.SuitEnum, double>();
            foreach (var suit in (CardService.SuitEnum[])Enum.GetValues(typeof(CardService.SuitEnum)))
            {
                if (suit == CardService.SuitEnum.None)
                    continue;

                double value = 0.0;
                foreach (var handCard in handCards.Where(_ => _.Card.Suit.Name == suit.ToString()))
                {
                    value += valueWorth[(CardService.ValueEnum)Enum.Parse(typeof(CardService.ValueEnum), handCard.Card.Value.Name)];
                }
                dictionary.Add(suit, value);
            }

            return dictionary;
        }

        public async Task<int> PlayCard(DbContextOptions<CardContext> options, int gameId, int playerId)
        {

            return 0;
        }

        private void SetAggressiveness(int playerSeat, int team1Points, int team2Points)
        {
            double difference = team1Points - team2Points;
            if (playerSeat == 1 || playerSeat == 3)
                difference = team2Points - team1Points;

            difference = difference / 11;

            // if number is greater than 0
                // the closer to zero, the more aggressive

            // if the number is less than 0
                // the closer to zero, the less aggressive

        }
    }
}
