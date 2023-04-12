using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using TipBot.Mongo;
using TipBot.TransferHelper;
namespace TipBot.Log {
    public enum TransactionType {
        Deposit,
        Transfer,
        Withdrawal,
        Game,
        Blackjack,
        Convert,
        Burn,
        Airdrop,
        ConvertToMili,
        ConvertFromMili,
        NFTTransfer
    }

    public class TransactionLog {
        public ObjectId id;
        public ulong Sender;
        public ulong[] Recipients;
        public Token Token;
        public string Total;
        public string Share;
        public string Hash;
        public TransactionType Type;
        public TransactionLog(ulong s, ulong[] r, Token t, string total, string share, TransactionType type, string h = "") {
            id = ObjectId.GenerateNewId();
            Sender = s;
            Recipients = r;
            Token = t;
            Total = total;
            Share = share;
            Type = type;
            Hash = h;
        }

        public static async Task LogTransation(ulong s, ulong[] r, Token t, string total, string share, TransactionType type, string h = "") {
            var tx = new TransactionLog(s, r, t, total, share, type, h);
            var logCollec = DatabaseConnection.GetDb().GetCollection<TransactionLog>("TransactionLogs");
            await logCollec.InsertOneAsync(tx);
        }

        public static async Task<bool> CheckifEventIsLogged(string hash) {
            var logCollec = DatabaseConnection.GetDb().GetCollection<TransactionLog>("TransactionLogs");
            var tx = (await logCollec.FindAsync(t => t.Hash == hash)).FirstOrDefault();
            return tx != null;
        }
    }
}
