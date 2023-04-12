using System;
using System.Linq;
using MongoDB.Driver;
using System.Threading.Tasks;
using TipBot.Mongo;
using System.Collections.Generic;
using Org.BouncyCastle.Bcpg;
using TipBot.TransferHelper;

namespace TipBot {
    // Divide by 10000 to obtain actual values
    public class AdminParametres {
        public int id;
        public int HouseFee;
        public int MaxBetPercentage;
        public bool IsGamblingEnabled;
        public Dictionary<string, int> TokenFeeReductions;

        private static AdminParametres Instance = null;

        public static async Task<AdminParametres> GetParams() {
            if (Instance == null) {
                var collec = DatabaseConnection.GetDb().GetCollection<AdminParametres>("AdminParametres");
                Instance = (await collec.FindAsync(p => p.id == 1)).FirstOrDefault();
            }
            return Instance;
        }

        public static async Task SetTokenFeeValue(int value, string symbol) {
            var token = await ServiceData.GetTokenSymbol(symbol);
            if (token == null || value < 0 || value > 10000)
                return;
            value = 10000 - value;
            var collec = DatabaseConnection.GetDb().GetCollection<AdminParametres>("AdminParametres");
            var instance = await GetParams();
            if (instance.TokenFeeReductions.ContainsKey(token.Symbol))
                instance.TokenFeeReductions[token.Symbol] = value;
            else
                instance.TokenFeeReductions.Add(token.Symbol, value);
            var update = Builders<AdminParametres>.Update.Set(t => t.TokenFeeReductions, instance.TokenFeeReductions);
            await collec.FindOneAndUpdateAsync(p => p.id == 1, update);
        }

        public static async Task<int> GetTokenFee(string symbol) {
            var token = await ServiceData.GetTokenSymbol(symbol);
            if (token == null)
                return 10000;
            var instance = await GetParams();
            if (instance.TokenFeeReductions.ContainsKey(token.Symbol))
                return instance.TokenFeeReductions[token.Symbol];
            else
                return 10000;
        }

        public static async Task SetHouseFee(int newFee) {
            var collec = DatabaseConnection.GetDb().GetCollection<AdminParametres>("AdminParametres");
            var instance = await GetParams();
            instance.HouseFee = newFee;
            var update = Builders<AdminParametres>.Update.Set(t => t.HouseFee, newFee);
            await collec.FindOneAndUpdateAsync(p => p.id == 1, update);
        }

        public static async Task SetMaxBet(int newMaxBet) {
            var collec = DatabaseConnection.GetDb().GetCollection<AdminParametres>("AdminParametres");
            var instance = await GetParams();
            instance.MaxBetPercentage = newMaxBet;
            var update = Builders<AdminParametres>.Update.Set(t => t.MaxBetPercentage, newMaxBet);
            await collec.FindOneAndUpdateAsync(p => p.id == 1, update);
        }

        public static async Task SetGambling(bool gambling) {
            var collec = DatabaseConnection.GetDb().GetCollection<AdminParametres>("AdminParametres");
            var instance = await GetParams();
            instance.IsGamblingEnabled = gambling;
            var update = Builders<AdminParametres>.Update.Set(t => t.IsGamblingEnabled, gambling);
            await collec.FindOneAndUpdateAsync(p => p.id == 1, update);
        }
    }
}
