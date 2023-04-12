using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Driver;
using TipBot.Mongo;
using System.Numerics;
using TipBot.TransferHelper;

namespace TipBot.Games {
    public class TipPlayerStats {
        public ulong id;
        public Dictionary<string, string> GambledAmounts;
        public Dictionary<string, string> WonAmounts;
        public Dictionary<string, string> LargestGainAmounts;
        public Dictionary<string, long> GamesPlayed;

        public TipPlayerStats(ulong _id, string symbol, BigInteger value, BigInteger gain) {
            id = _id;
            GambledAmounts = new Dictionary<string, string>();
            WonAmounts = new Dictionary<string, string>();
            LargestGainAmounts = new Dictionary<string, string>();
            GamesPlayed = new Dictionary<string, long>();
            GambledAmounts.Add(symbol, value.ToString());
            WonAmounts.Add(symbol, gain.ToString());
            LargestGainAmounts.Add(symbol, gain.ToString());
            GamesPlayed.Add(symbol, 1);
        }
        public static async Task<TipPlayerStats> GetStats(ulong id) {
            var collec = DatabaseConnection.GetDb().GetCollection<TipPlayerStats>("PlayerStats");
            return (await collec.FindAsync(w => w.id == id)).FirstOrDefault();
        }
        public static async Task LogBetToDatabase(ulong id, string symbol, BigInteger value, BigInteger gain) {
            var collec = DatabaseConnection.GetDb().GetCollection<TipPlayerStats>("PlayerStats");
            var token = await ServiceData.GetTokenSymbol(symbol);
            var data = await GetStats(id);
            if (data == null) {
                data = new TipPlayerStats(id, token.Symbol, value, gain);
                await collec.InsertOneAsync(data);
            }
            else {
                if (data.GamesPlayed.ContainsKey(token.Symbol)) {
                    data.GamesPlayed[token.Symbol]++;
                }
                else
                    data.GamesPlayed.Add(token.Symbol, 1);
                if (data.GambledAmounts.ContainsKey(token.Symbol))
                    data.GambledAmounts[token.Symbol] = (BigInteger.Parse(data.GambledAmounts[token.Symbol]) + value).ToString();
                else
                    data.GambledAmounts.Add(token.Symbol, value.ToString());
                if (data.WonAmounts.ContainsKey(token.Symbol))
                    data.WonAmounts[token.Symbol] = (BigInteger.Parse(data.WonAmounts[token.Symbol]) + gain).ToString();
                else
                    data.WonAmounts.Add(token.Symbol, gain.ToString());
                if (data.LargestGainAmounts.ContainsKey(token.Symbol)) {
                    if (BigInteger.Parse(data.LargestGainAmounts[token.Symbol]) < gain)
                        data.LargestGainAmounts[token.Symbol] = gain.ToString();
                }
                else
                    data.LargestGainAmounts.Add(token.Symbol, gain.ToString());
                await collec.ReplaceOneAsync(s => s.id == id, data);
            }
        }
    }
}
