using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Driver;
using TipBot.Mongo;

namespace TipBot.TransferHelper {
    public class Token {
        public string ContractAddress;
        public string Symbol;
        public int Decimal;
        public string Emote;
        public Token(string add, string sym, int dec, string emote) {
            ContractAddress = add;
            Symbol = sym;
            Decimal = dec;
            Emote = emote;
        }
    }

    public class NFT {
        public string ContractAddress;
        public string Symbol;
        public string Emote;
        public NFT(string add, string sym, string emote) {
            ContractAddress = add;
            Symbol = sym;
            Emote = emote;
        }
    }
    public class ServiceData {
        public int id;
        public List<Token> tokenList;
        public List<NFT> NFTList;

        public static async Task<ServiceData> GetList() {
            var tokensCollec = DatabaseConnection.GetDb().GetCollection<ServiceData>("ServiceData");
            var data = (await tokensCollec.FindAsync(w => w.id == 1)).FirstOrDefault();
            return data;
        }

        public async Task SaveData() {
            var tokensCollec = DatabaseConnection.GetDb().GetCollection<ServiceData>("ServiceData");
            await tokensCollec.FindOneAndReplaceAsync(l => l.id == 1, this);
        }

        public static async Task CreateDoc() {
            var doc = new ServiceData();
            doc.id = 1;
            doc.tokenList = new List<Token>();
            var tokensCollec = DatabaseConnection.GetDb().GetCollection<ServiceData>("ServiceData");
            await tokensCollec.InsertOneAsync(doc);
        }

        public static async Task<Token> GetToken(string add) {
            var data = await GetList();
            var token = data.tokenList.FirstOrDefault(t => t.ContractAddress.ToLower() == add.ToLower());
            return token;
        }

        public static async Task<Token> GetTokenSymbol(string sym) {
            var data = await GetList();
            var token = data.tokenList.FirstOrDefault(t => t.Symbol.ToLower() == sym.ToLower());
            return token;
        }

        public static async Task<NFT> GetNFTToken(string add) {
            var data = await GetList();
            var token = data.NFTList.FirstOrDefault(t => t.ContractAddress.ToLower() == add.ToLower());
            return token;
        }

        public static async Task<NFT> GetNFTTokenSymbol(string sym) {
            var data = await GetList();
            var token = data.NFTList.FirstOrDefault(t => t.Symbol.ToLower() == sym.ToLower());
            return token;
        }
    }
}
