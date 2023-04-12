using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Numerics;
using MongoDB.Bson;
using MongoDB.Driver;
using TipBot.Mongo;
using TipBot.TransferHelper;

namespace TipBot {
    /*
     * We are using string for Value as token values can exceed int64
     */
    public class TipWallet {
        public ObjectId id;
        public string ContractAddress;
        public string Symbol;
        public int Decimal;
        public string Value;

        public TipWallet(string add, string sym, int dec) {
            id = ObjectId.GenerateNewId();
            ContractAddress = add;
            Symbol = sym;
            Decimal = dec;
            Value = "0";
        }

        public void Add(BigInteger value) {
            var currentValue = BigInteger.Parse(Value);
            currentValue += value;
            Value = currentValue.ToString();
        }

        public void Sub(BigInteger value) {
            var currentValue = BigInteger.Parse(Value);
            currentValue -= value;
            Value = currentValue.ToString();
        }

        public string ParseValue(string value) {
            int currentDecimals;
            int periodIndex = -1;
            for (int i = 0; i < value.Length; i++) {
                if (value[i] == '.') {
                    periodIndex = i;
                    break;
                }
            }
            if (periodIndex != -1)
                currentDecimals = value.Length - periodIndex - 1;
            else
                currentDecimals = 0;
            value = value.Replace(".", "");
            return value.PadRight(value.Length + Decimal - currentDecimals, '0');
        }

        public static string ParseValueToTokenDecimal(string value, int dec) {
            int currentDecimals;
            int periodIndex = -1;
            for (int i = 0; i < value.Length; i++) {
                if (value[i] == '.') {
                    periodIndex = i;
                    break;
                }
            }
            if (periodIndex != -1)
                currentDecimals = value.Length - periodIndex - 1;
            else
                currentDecimals = 0;
            value = value.Replace(".", "");
            return value.PadRight(value.Length + dec - currentDecimals, '0');
        }

        public static bool IsValidValue(string value, int dec) {
            int periodCount = 0;
            int periodIndex = -1;
            for (int i = 0; i < value.Length; i++) {
                if (!char.IsNumber(value[i])) {
                    if (value[i] != '.')
                        return false;
                    else {
                        periodCount++;
                        periodIndex = i;
                    }
                }
                if (periodCount > 1)
                    return false;
            }
            if (periodIndex != -1 && periodIndex != value.Length - 1)
                if (value.Length - periodIndex - 1 > dec)
                    return false;
            return true;
        }

        public bool IsValidValue(string value) {
            int periodCount = 0;
            int periodIndex = -1;
            for (int i = 0; i < value.Length; i++) {
                if (!char.IsNumber(value[i])) {
                    if (value[i] != '.')
                        return false;
                    else {
                        periodCount++;
                        periodIndex = i;
                    }
                }
                if (periodCount > 1)
                    return false;
            }
            if (periodIndex != -1 && periodIndex != value.Length - 1)
                if (value.Length - periodIndex - 1 > Decimal)
                    return false;
            return true;
        }

        public async Task SaveToDatabase() {
            var walletCollec = DatabaseConnection.GetDb().GetCollection<TipWallet>("Wallets");
            var data = (await walletCollec.FindAsync(w => w.id == id)).FirstOrDefault();
            if (data != null) {
                var update = Builders<TipWallet>.Update.Set(t => t.Value, Value);
                await walletCollec.FindOneAndUpdateAsync(w => w.id == id, update);
            }
            else
                await walletCollec.InsertOneAsync(this);
        }

        public async Task SaveNameToDatabase() {
            var walletCollec = DatabaseConnection.GetDb().GetCollection<TipWallet>("Wallets");
            var data = (await walletCollec.FindAsync(w => w.id == id)).FirstOrDefault();
            if (data != null) {
                var update = Builders<TipWallet>.Update.Set(t => t.Symbol, Symbol);
                await walletCollec.FindOneAndUpdateAsync(w => w.id == id, update);
            }
            else
                await walletCollec.InsertOneAsync(this);
        }

        public async Task SaveToDatabase(IClientSessionHandle session) {
            var walletCollec = DatabaseConnection.GetDb().GetCollection<TipWallet>("Wallets");
            var data = (await walletCollec.FindAsync(w => w.id == id)).FirstOrDefault();
            if (data != null) {
                var update = Builders<TipWallet>.Update.Set(t => t.Value, Value);
                await walletCollec.FindOneAndUpdateAsync(session, w => w.id == id, update);
            }
            else
                await walletCollec.InsertOneAsync(session, this);
        }

        public async Task DeleteFromDatabase(IClientSessionHandle session) {
            var walletCollec = DatabaseConnection.GetDb().GetCollection<TipWallet>("Wallets");
            await walletCollec.DeleteOneAsync(session, w => w.id == id);
        }

        public async Task DeleteFromDatabase() {
            var walletCollec = DatabaseConnection.GetDb().GetCollection<TipWallet>("Wallets");
            await walletCollec.DeleteOneAsync(w => w.id == id);
        }

        public static async Task<TipWallet> GetWallet(string _id) {
            var walletCollec = DatabaseConnection.GetDb().GetCollection<TipWallet>("Wallets");
            return (await walletCollec.FindAsync(w => w.id == ObjectId.Parse(_id))).FirstOrDefault();
        }

        public static async Task<List<TipWallet>> GetListOfWalletsOfOneToken(Token token) {
            var walletCollec = DatabaseConnection.GetDb().GetCollection<TipWallet>("Wallets");
            return await (await walletCollec.FindAsync(w => w.Symbol == token.Symbol)).ToListAsync();
        }

        public static async Task<string> GetWalletValue(string _id, bool noSpace = false) {
            var wallet = await GetWallet(_id);
            return TransferFunctions.FormatUint(wallet.Value, wallet.Decimal, noSpace);
        }

        public string GetWallerValue() {
            return TransferFunctions.FormatUint(Value, Decimal, false);
        }

        public bool HasFunds(string value) {
            return BigInteger.Parse(ParseValue(value)) <= BigInteger.Parse(Value);
        }

        public bool HasFunds(BigInteger value) {
            return value <= BigInteger.Parse(Value);
        }

        public static async Task Migrate() {
            var walletCollec = DatabaseConnection.GetDb().GetCollection<TipWallet>("Wallets");
            var slpUpdate = Builders<TipWallet>.Update.Set(u => u.ContractAddress, "0xa8754b9fa15fc18bb59458815510e40a12cd2014");
            var axsUpdate = Builders<TipWallet>.Update.Set(u => u.ContractAddress, "0x97a9107c1793bc407d6f527b77e7fff4d812bece");
            await walletCollec.UpdateManyAsync(u => u.Symbol == "AXS", axsUpdate);
            await walletCollec.UpdateManyAsync(u => u.ContractAddress == "SLP", slpUpdate);
        }
    }
}
