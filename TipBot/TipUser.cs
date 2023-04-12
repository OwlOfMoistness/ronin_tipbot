using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Driver;
using TipBot.Mongo;
namespace TipBot {
    public class TipUser {
        public ulong id;
        public string WithdrawalAddress;
        public Dictionary<string, string> walletList;

        public TipUser(ulong _id) {
            id = _id;
            WithdrawalAddress = "0x";
            walletList = new Dictionary<string, string>();
        }

        public async Task SaveWithdrawalAddress() {
            var userCollec = DatabaseConnection.GetDb().GetCollection<TipUser>("Users");
            var data = (await userCollec.FindAsync(w => w.id == id)).FirstOrDefault();
            if (data != null) {
                var update = Builders<TipUser>.Update.Set(t => t.WithdrawalAddress, WithdrawalAddress);
                await userCollec.FindOneAndUpdateAsync(w => w.id == id, update);
            }
            else
                await userCollec.InsertOneAsync(this);
        }

        public async Task SaveToDatabase() {
            var userCollec = DatabaseConnection.GetDb().GetCollection<TipUser>("Users");
            var data = (await userCollec.FindAsync(w => w.id == id)).FirstOrDefault();
            if (data != null) {
                var update = Builders<TipUser>.Update.Set(t => t.walletList, walletList);
                await userCollec.FindOneAndUpdateAsync(w => w.id == id, update);
            }
            else
                await userCollec.InsertOneAsync(this);
        }

        public async Task SaveToDatabase(IClientSessionHandle session) {
            var userCollec = DatabaseConnection.GetDb().GetCollection<TipUser>("Users");
            var data = (await userCollec.FindAsync(w => w.id == id)).FirstOrDefault();
            if (data != null) {
                var update = Builders<TipUser>.Update.Set(t => t.walletList, walletList);
                await userCollec.FindOneAndUpdateAsync(session, w => w.id == id, update);
            }
            else
                await userCollec.InsertOneAsync(session, this);
        }

        public static async Task<TipUser> GetUser(ulong _id) {
            var userCollec = DatabaseConnection.GetDb().GetCollection<TipUser>("Users");
            return (await userCollec.FindAsync(w => w.id == _id)).FirstOrDefault();
        }

        public static async Task<TipUser> GetUserFromWalletId(string _id, string sym) {
            var userCollec = DatabaseConnection.GetDb().GetCollection<TipUser>("Users");
            return (await userCollec.FindAsync(w => w.walletList[sym] == _id)).FirstOrDefault();
        }

        public static bool IsValidAddress(string address) {
            if (!address.StartsWith("ronin:") || address.Length != 46)
                return false;
            address = address.ToLower();
            for (int i = 6; i < address.Length; i++)
                if (!((address[i] >= '0' && address[i] <= '9') || (address[i] >= 'a' && address[i] <= 'f')))
                    return false;
            return true;
        }
    }
}
